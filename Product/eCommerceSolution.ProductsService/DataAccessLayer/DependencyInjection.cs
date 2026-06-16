using eCommerce.DataAccessLayer.Context;
using eCommerce.DataAccessLayer.Repositories;
using eCommerce.DataAccessLayer.RepositoryContracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace eCommerce.ProductsService.DataAccessLayer
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDataAccessLayer(this IServiceCollection services, IConfiguration configuration)
        {
            string connectionStringTemplate = configuration.GetConnectionString("DefaultConnection")!;
            string connectionString = connectionStringTemplate
               .Replace("$MYSQL_HOST", Environment.GetEnvironmentVariable("MYSQL_HOST") ?? "localhost");
               .Replace("$MYSQL_PASSWORD", Environment.GetEnvironmentVariable("MYSQL_PASSWORD") ?? "admin");
            
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseMySQL(connectionString);
            });

            services.AddScoped<IProductsRepository, ProductsRepository>();
            return services;
        }
    }
}