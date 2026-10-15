using eCommerce.API.Middlewares;
using eCommerce.Core;
using eCommerce.Core.DTO;
using eCommerce.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure();
builder.Services.AddCore();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

            var response = new ApiErrorResponse("Dados da requisição inválidos.", errors, "ModelBinding");

            return new BadRequestObjectResult(response);
        };
    });

// Microsoft Entra External ID (tenant externo, domínio ciamlogin.com).
// Valida o access token também aqui (defesa em profundidade: o APIM já validou antes).
// Sem EntraExternalId__ApiClientId (docker-compose), só os endpoints anônimos funcionam.
var entra = builder.Configuration.GetSection("EntraExternalId");
var entraTenantSubdomain = entra["TenantSubdomain"];
var entraTenantId = entra["TenantId"];
var entraApiClientId = entra["ApiClientId"];

var authenticationBuilder = builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);

if (!string.IsNullOrWhiteSpace(entraTenantSubdomain)
    && !string.IsNullOrWhiteSpace(entraTenantId)
    && !string.IsNullOrWhiteSpace(entraApiClientId))
{
    authenticationBuilder.AddJwtBearer(options =>
    {
        // O metadata deste authority devolve issuer https://<tenant-id>.ciamlogin.com/<tenant-id>/v2.0,
        // que é exatamente o "iss" dos tokens v2 do tenant externo.
        options.Authority = $"https://{entraTenantSubdomain}.ciamlogin.com/{entraTenantId}/v2.0";
        // Token v2 (requestedAccessTokenVersion = 2 no manifest): "aud" = Client ID da API.
        options.Audience = entraApiClientId;
        // Mantém os nomes curtos das claims (oid, email, name, roles, scp).
        options.MapInboundClaims = false;
        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.RoleClaimType = "roles";
    });
}

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseExceptionHandlingMiddleware();

app.UseRouting();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.Run();
