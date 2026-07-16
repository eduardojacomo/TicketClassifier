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

// ── Layers: Repository → Service ─────────────────────────────────────────────
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<CsvService>();
builder.Services.AddSingleton<IProgressStore, ProgressStore>();

// ── Classification gateways (strategies) + Factory ───────────────────────────
builder.Services.AddHttpClient<AnthropicGateway>(c => c.Timeout = TimeSpan.FromSeconds(60));
builder.Services.AddHttpClient<GeminiGateway>(c => c.Timeout = TimeSpan.FromSeconds(60));
builder.Services.AddHttpClient<LlamaGateway>(c => c.Timeout = TimeSpan.FromMinutes(10));
builder.Services.AddScoped<IClassificationGatewayFactory, ClassificationGatewayFactory>();

var app = builder.Build();

// Diagnostics: shows what the configuration ACTUALLY loaded (masked).
var geminiKey = app.Configuration["Llm:Gemini:ApiKey"] ?? "";
var maskedKey = geminiKey.Length <= 8 ? geminiKey : $"{geminiKey[..4]}…{geminiKey[^4..]}";
app.Logger.LogInformation(
    "Startup: environment={Env} | Llm:Provider={Provider} | GeminiKey(len={Len})='{Key}'",
    app.Environment.EnvironmentName,
    app.Configuration["Llm:Provider"],
    geminiKey.Length,
    string.IsNullOrEmpty(geminiKey) ? "(empty)" : maskedKey);

// Applies pending migrations at startup (waits for Postgres to come up).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    for (var i = 0; i < 10; i++)
    {
        try
        {
            // If the database already exists (created by the old EnsureCreated) but doesn't have
            // the history table, mark InitialCreate as already applied.
            var pendingMigrations = db.Database.GetPendingMigrations().ToList();
            if (pendingMigrations.Contains("20260714210202_InitialCreate"))
            {
                var tables = db.Database.SqlQueryRaw<string>(
                    "SELECT tablename FROM pg_tables WHERE schemaname = 'public'").ToList();
                if (tables.Contains("Batches"))
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
            app.Logger.LogWarning(ex, "Waiting for database (attempt {N}/10)...", i + 1);
            Thread.Sleep(2000);
        }
    }

    // Seed classification parameters (only if the table is empty)
    try
    {
        var repo = scope.ServiceProvider.GetRequiredService<ITicketRepository>();
        var count = await repo.CountParametersAsync();
        if (count == 0)
        {
            var seed = ParameterSeed.Generate();
            await repo.AddParametersAsync(seed);
            app.Logger.LogInformation("Seed: {Count} classification parameters inserted.", seed.Count);
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Failed to execute parameter seed.");
    }
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors(CorsPolicy);
app.MapControllers();
app.MapGet("/", () => Results.Ok(new { status = "ok", service = "TicketClassifier API" }));

app.Run();
