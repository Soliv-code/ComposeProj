using Microsoft.EntityFrameworkCore;
using Products.API.Database;

namespace Products.API.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddDbContext<ApplicationDbContext>(opt =>
        {
            opt.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
            });

            if (environment.IsDevelopment())
            {
                opt.EnableSensitiveDataLogging();
                opt.EnableDetailedErrors();
            }
        });

        return services;
    }
}