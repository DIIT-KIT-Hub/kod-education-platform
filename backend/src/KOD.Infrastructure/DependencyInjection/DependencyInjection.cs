using KOD.Application.Abstractions.Persitence.Transactions;
using KOD.Application.Abstractions.Services.Auth;
using KOD.Application.Abstractions.Services.Emails;
using KOD.Application.Abstractions.Services.Identity;
using KOD.Application.Abstractions.Services.Otp;
using KOD.Domain.Entities.Users;
using KOD.Domain.Repositories;
using KOD.Infrastructure.Implementations.Persistence.Database;
using KOD.Infrastructure.Implementations.Persistence.Repositories.Auth;
using KOD.Infrastructure.Implementations.Persistence.Repositories.Identity;
using KOD.Infrastructure.Implementations.Persistence.Repositories.Otp;
using KOD.Infrastructure.Implementations.Persistence.Transactions;
using KOD.Infrastructure.Implementations.Services.Auth;
using KOD.Infrastructure.Implementations.Services.Emails;
using KOD.Infrastructure.Implementations.Services.Identity;
using KOD.Infrastructure.Implementations.Services.Otp;
using KOD.Infrastructure.Options.Auth;
using KOD.Infrastructure.Options.Emails;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KOD.Infrastructure.DependencyInjection;

/// <summary>
/// Provides extension methods to register infrastructure services, repositories, database, and options into the dependency injection container.
/// </summary>
public static class DependencyInjection
{
    #region Public methods

    /// <summary>
    /// Adds the infrastructure layer services, database context, repositories, and options to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        RegisterOptions(services, configuration);

        RegisterDatabase(services, configuration);

        RegisterRepositories(services);

        RegisterServices(services);

        return services;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Registers configuration options like JWT and Email settings.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    private static void RegisterOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

        services.Configure<EmailOptions>(configuration.GetSection("Email"));
    }

    /// <summary>
    /// Registers the database context and ASP.NET Identity services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    private static void RegisterDatabase(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
         options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
    }

    /// <summary>
    /// Registers repository implementations for dependency injection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    private static void RegisterRepositories(IServiceCollection services)
    {
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IIdentityRepository, IdentityRepository>();
        services.AddScoped<IOtpRepository, OtpRepository>();
    }

    /// <summary>
    /// Registers service implementations for dependency injection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    private static void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<ITransactionManager, TransactionManager>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IAuthService, AuthService>();
    }

    #endregion
}
