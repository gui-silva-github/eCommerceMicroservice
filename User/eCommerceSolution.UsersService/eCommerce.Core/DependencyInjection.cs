using eCommerce.Core.Mappers;
using eCommerce.Core.ServiceContracts;
using eCommerce.Core.Services;
using eCommerce.Core.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace eCommerce.Core
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddCore(this IServiceCollection services)
        {
            services.AddAutoMapper(cfg =>
            {
                cfg.AddProfile<ApplicationUserMappingProfile>();
                cfg.AddProfile<RegisterRequestMappingProfile>();
            });

            services.AddTransient<IUsersService, UsersService>();
            services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

            return services;
        }
    }
}
