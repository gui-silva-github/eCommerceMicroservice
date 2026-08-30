using eCommerce.OrdersMicroservice.BusinessLogicLayer.HttpClients;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.Mappers;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.Policies;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.RabbitMQ;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.ServiceContracts;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.Services;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.Validators;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddBusinessLogicLayer(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddAutoMapper(cfg =>
            {
                cfg.AddMaps(typeof(OrderAddRequestToOrderMappingProfile).Assembly);
            });

            services.AddValidatorsFromAssemblyContaining<OrderAddRequestValidator>();

            string usersBaseUrl = configuration["UsersMicroserviceBaseUrl"]
                ?? throw new InvalidOperationException("UsersMicroserviceBaseUrl não configurada.");
            string productsBaseUrl = configuration["ProductsMicroserviceBaseUrl"]
                ?? throw new InvalidOperationException("ProductsMicroserviceBaseUrl não configurada.");

            ResilienceOptions resilienceOptions = configuration
                .GetSection(ResilienceOptions.SectionName)
                .Get<ResilienceOptions>() ?? new ResilienceOptions();

            services.AddSingleton(resilienceOptions);

            IAsyncPolicy<HttpResponseMessage> usersPolicy =
                ResiliencePolicies.CreateCombinedPolicy("UsersService", resilienceOptions);
            IAsyncPolicy<HttpResponseMessage> productsPolicy =
                ResiliencePolicies.CreateCombinedPolicy("ProductsService", resilienceOptions);

            // Dependant (Orders) → Dependency (Users / Products).
            // HttpClientFactory + AddPolicyHandler (WaitAndRetry, Timeout, CircuitBreaker, Bulkhead, Fallback).
            services.AddHttpClient<IUsersMicroserviceClient, UsersMicroserviceClient>(client =>
            {
                client.BaseAddress = new Uri(usersBaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromMinutes(2);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            })
            .AddPolicyHandler(usersPolicy);

            services.AddHttpClient<IProductsMicroserviceClient, ProductsMicroserviceClient>(client =>
            {
                client.BaseAddress = new Uri(productsBaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromMinutes(2);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            })
            .AddPolicyHandler(productsPolicy);

            RabbitMQOptions rabbitMQOptions = configuration
                .GetSection(RabbitMQOptions.SectionName)
                .Get<RabbitMQOptions>() ?? new RabbitMQOptions();

            services.AddSingleton(rabbitMQOptions);
            services.AddHostedService<RabbitMQProductConsumerHostedService>();

            services.AddScoped<IOrdersService, OrdersService>();

            return services;
        }
    }
}
