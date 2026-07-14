using Microsoft.EntityFrameworkCore;
using TicketClassifier.Api.Data;
using TicketClassifier.Api.Gateways;
using TicketClassifier.Api.Gateways.Interface;
using TicketClassifier.Api.Repositories;
using TicketClassifier.Api.Repositories.Interface;
using TicketClassifier.Api.Services;
using TicketClassifier.Api.Services.Interface;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicy = "spa";
builder.Services.AddCors(o => o.AddPolicy(CorsPolicy, p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("DataBase")));

// ── Camadas: Repositório → Serviço ───────────────────────────────────────────
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<CsvService>();
builder.Services.AddSingleton<IProgressoStore, ProgressoStore>();

// ── Gateways de classificação (estratégias) + Factory ───────────────────────
builder.Services.AddHttpClient<AnthropicGateway>(c => c.Timeout = TimeSpan.FromSeconds(60));
builder.Services.AddHttpClient<GeminiGateway>(c => c.Timeout = TimeSpan.FromSeconds(60));
builder.Services.AddHttpClient<LlamaGateway>(c => c.Timeout = TimeSpan.FromMinutes(10));
builder.Services.AddScoped<IClassificacaoGatewayFactory, ClassificacaoGatewayFactory>();

var app = builder.Build();

// Diagnóstico: mostra o que a configuração REALMENTE carregou (mascarado).
var geminiKey = app.Configuration["Llm:Gemini:ApiKey"] ?? "";
var mascarada = geminiKey.Length <= 8 ? geminiKey : $"{geminiKey[..4]}…{geminiKey[^4..]}";
app.Logger.LogInformation(
    "Startup: ambiente={Env} | Llm:Provider={Provider} | GeminiKey(len={Len})='{Key}'",
    app.Environment.EnvironmentName,
    app.Configuration["Llm:Provider"],
    geminiKey.Length,
    string.IsNullOrEmpty(geminiKey) ? "(vazia)" : mascarada);

// Aplica migrations pendentes no startup (aguarda o Postgres subir).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    for (var i = 0; i < 10; i++)
    {
        try
        {
            // Se o banco já existe (criado pelo EnsureCreated antigo) mas não tem
            // a tabela de histórico, marca a InitialCreate como já aplicada.
            var pendentes = db.Database.GetPendingMigrations().ToList();
            if (pendentes.Contains("20260714210202_InitialCreate"))
            {
                var tabelas = db.Database.SqlQueryRaw<string>(
                    "SELECT tablename FROM pg_tables WHERE schemaname = 'public'").ToList();
                if (tabelas.Contains("Batches"))
                {
                    db.Database.ExecuteSqlRaw(
                        """
                        CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                            "MigrationId" varchar(150) NOT NULL PRIMARY KEY,
                            "ProductVersion" varchar(32) NOT NULL
                        );
                        INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                        VALUES ('20260714210202_InitialCreate', '8.0.10')
                        ON CONFLICT DO NOTHING;
                        """);
                }
            }

            db.Database.Migrate();
            break;
        }
        catch (Exception ex) when (i < 9)
        {
            app.Logger.LogWarning(ex, "Aguardando banco de dados (tentativa {N}/10)...", i + 1);
            Thread.Sleep(2000);
        }
    }

    // Seed dos parâmetros de classificação (apenas se a tabela estiver vazia)
    try
    {
        var repo = scope.ServiceProvider.GetRequiredService<ITicketRepository>();
        var count = await repo.ContarParametrosAsync();
        if (count == 0)
        {
            var seed = ParametroSeed.Gerar();
            await repo.AdicionarParametrosAsync(seed);
            app.Logger.LogInformation("Seed: {Count} parâmetros de classificação inseridos.", seed.Count);
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Falha ao executar seed dos parâmetros.");
    }
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors(CorsPolicy);
app.MapControllers();
app.MapGet("/", () => Results.Ok(new { status = "ok", servico = "TicketClassifier API" }));

app.Run();
