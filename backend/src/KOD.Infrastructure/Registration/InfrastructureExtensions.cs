using KOD.Infrastructure.Persistence.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KOD.Infrastructure.Registration;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        RegisterDatabase(services, configuration);

        return services;
    }

    private static void RegisterDatabase(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PostgresDbContext>(options =>
         options.UseNpgsql(configuration.GetConnectionString("PostgresConnection")));
    }
}