using eCommerce.BusinessLogicLayer.Mappers;
using eCommerce.BusinessLogicLayer.RabbitMQ;
using eCommerce.BusinessLogicLayer.ServiceContracts;
using eCommerce.BusinessLogicLayer.Validators;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace eCommerce.ProductsService.BusinessLogicLayer
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddBusinessLogicLayer(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddAutoMapper(typeof(ProductAddRequestToProductMappingProfile).Assembly);

            services.AddValidatorsFromAssemblyContaining<ProductAddRequestValidator>();

            RabbitMQOptions rabbitMQOptions = configuration
                .GetSection(RabbitMQOptions.SectionName)
                .Get<RabbitMQOptions>() ?? new RabbitMQOptions();

            services.AddSingleton(rabbitMQOptions);
            services.AddSingleton<IRabbitMQPublisher, RabbitMQPublisher>();

            services.AddScoped<IProductsService, eCommerce.BusinessLogicLayer.Services.ProductsService>();

            return services;
        }
    }
}