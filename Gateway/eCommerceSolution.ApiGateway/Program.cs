using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;

var builder = WebApplication.CreateBuilder(args);

string ocelotFile = builder.Environment.IsEnvironment("Docker")
    ? "ocelot.docker.json"
    : "ocelot.json";

builder.Configuration.AddJsonFile(ocelotFile, optional: false, reloadOnChange: true);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200")
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
