using eCommerce.OrdersMicroservice.BusinessLogicLayer.HttpClients;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.Mappers;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.ServiceContracts;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.Services;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.Validators;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

            // Comunicação síncrona entre microserviços via typed HttpClient.
            // Aceita certificado de desenvolvimento do localhost (dotnet dev-certs).
            services.AddHttpClient<IUsersMicroserviceClient, UsersMicroserviceClient>(client =>
            {
                client.BaseAddress = new Uri(usersBaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });

            services.AddHttpClient<IProductsMicroserviceClient, ProductsMicroserviceClient>(client =>
            {
                client.BaseAddress = new Uri(productsBaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });

            services.AddScoped<IOrdersService, OrdersService>();

            return services;
        }
    }
}
