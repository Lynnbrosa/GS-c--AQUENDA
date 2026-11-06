using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Aquenda.Api.Hubs;
using Aquenda.Api.Infra;
using Aquenda.Application.Chat;
using Aquenda.Application;
using Aquenda.Infrastructure;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Todo log do console passa pelo mascarador de PII (CPF, telefone, e-mail, nome de registro, segredo, saúde).
builder.Logging.AddConsole(o => o.FormatterName = FormatadorLogSemPii.Nome)
    .AddConsoleFormatter<FormatadorLogSemPii, Microsoft.Extensions.Logging.Console.ConsoleFormatterOptions>();

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Aquenda API",
        Version = "v1",
        Description = "Catálogo de serviços LGBTQIAPN+ de São Paulo, vagas das casas de acolhimento, " +
                      "salas de conversa moderadas (tempo real em /hubs/chat), rodas, curadoria e " +
                      "indicador de impacto anônimo. GS CitizenTech FIAP 2026."
    });
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
    c.AddSecurityDefinition(ChaveOperadorSwaggerFilter.Esquema, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = ChaveOperadorAttribute.Cabecalho,
        Description = "Chave das rotas de escrita (casa ou ONG)."
    });
    c.OperationFilter<ChaveOperadorSwaggerFilter>();
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TratadorDeErros>();

builder.Services.AddAquendaApplication();
builder.Services.AddAquendaInfrastructure(builder.Configuration);

builder.Services.AddSignalR()
    .AddJsonProtocol(o => o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddScoped<INotificadorChat, NotificadorSignalR>();

builder.Services.AddHealthChecks().AddCheck<BancoHealthCheck>("banco");

// O app Expo (web) e o painel chamam de outra origem.
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().WithMethods("GET", "POST", "DELETE")));

// Limite para rotas anônimas de escrita. A contagem fica só na memória e some a cada minuto.
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy(Program.LimiteAnonimo, http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1) }));
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Configuration.GetValue("Swagger:Habilitado", true))
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.DocumentTitle = "Aquenda API");
}

app.UseCors();
app.UseRateLimiter();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

if (app.Configuration.GetValue("Banco:InicializarAoSubir", true))
    await app.Services.InicializarBancoAsync(semear: app.Configuration.GetValue("Banco:Semear", true));

await app.RunAsync();

public partial class Program
{
    public const string LimiteAnonimo = "anonimo";
}
