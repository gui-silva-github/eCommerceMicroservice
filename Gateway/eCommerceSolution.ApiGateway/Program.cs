using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;

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

await app.UseOcelot();

app.Run();
