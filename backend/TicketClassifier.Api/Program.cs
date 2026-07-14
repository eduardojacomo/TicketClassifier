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
builder.Services.AddScoped<MockGateway>();
builder.Services.AddHttpClient<AnthropicGateway>(c => c.Timeout = TimeSpan.FromSeconds(60));
builder.Services.AddHttpClient<GeminiGateway>(c => c.Timeout = TimeSpan.FromSeconds(60));
builder.Services.AddHttpClient<LlamaGateway>(c => c.Timeout = TimeSpan.FromSeconds(120));
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

// Cria o schema no startup (scaffold — aguarda o Postgres subir).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    for (var i = 0; i < 10; i++)
    {
        try { db.Database.EnsureCreated(); break; }
        catch { Thread.Sleep(2000); }
    }
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors(CorsPolicy);
app.MapControllers();
app.MapGet("/", () => Results.Ok(new { status = "ok", servico = "TicketClassifier API" }));

app.Run();
