using eCommerce.BusinessLogicLayer.Mappers;
using eCommerce.BusinessLogicLayer.ServiceContracts;
using Microsoft.Extensions.DependencyInjection;
using eCommerce.BusinessLogicLayer.Validators;
using FluentValidation;

namespace eCommerce.ProductsService.BusinessLogicLayer
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddBusinessLogicLayer(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(ProductAddRequestToProductMappingProfile).Assembly);

            services.AddValidatorsFromAssemblyContaining<ProductAddRequestValidator>();

            services.AddScoped<IProductsService, eCommerce.BusinessLogicLayer.Services.ProductsService>();

            return services;
        }
    }
}