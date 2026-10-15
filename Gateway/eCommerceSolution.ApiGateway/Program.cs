using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

string ocelotFile = builder.Environment.EnvironmentName switch
{
    "Docker" => "ocelot.docker.json",
    "Aks" => "ocelot.docker.aks.json",
    _ => "ocelot.json"
};

builder.Configuration.AddJsonFile(ocelotFile, optional: false, reloadOnChange: true);

// Origens do Angular: local (ng serve) + opcional via Cors__Origins (CSV) no Azure.
var corsOrigins = builder.Configuration["Cors:Origins"]
    ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? ["http://localhost:4200", "http://127.0.0.1:4200"];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Upstream (frontend) → Downstream (microserviços) + QoS Polly (timeout / circuit breaker).
builder.Services.AddOcelot(builder.Configuration).AddPolly();

var app = builder.Build();

app.UseCors();

app.MapWhen(
    context => context.Request.Path.StartsWithSegments("/health"),
    health => health.Run(async context =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(
            """{"status":"ok","service":"api-gateway"}""");
    }));

// Com o APIM na frente: só aceita chamadas que tragam o segredo que o APIM injeta
// (named value "gateway-shared-secret" → header X-Apim-Secret). Assim ninguém pula o APIM
// chamando o FQDN do ACA / IP do Ingress direto. Sem Apim__SharedSecret (Compose), nada muda.
var apimSharedSecret = builder.Configuration["Apim:SharedSecret"];

if (!string.IsNullOrWhiteSpace(apimSharedSecret))
{
    byte[] expectedSecret = Encoding.UTF8.GetBytes(apimSharedSecret);

    app.Use(async (context, next) =>
    {
        string received = context.Request.Headers["X-Apim-Secret"].ToString();

        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(received), expectedSecret))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { message = "Acesse a API pelo Azure API Management." });
            return;
        }

        // Não repassa o segredo para os microserviços.
        context.Request.Headers.Remove("X-Apim-Secret");
        await next();
    });
}

await app.UseOcelot();

app.Run();
