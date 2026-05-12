using KOD.Application.Abstractions.Services.Auth;
using KOD.Application.Abstractions.Services.Identity;
using KOD.Application.Abstractions.Services.Otp;

using Microsoft.Extensions.DependencyInjection;

namespace KOD.Application.DependencyInjection;

/// <summary>
/// Provides extension methods for registering application layer services.
/// </summary>
public static class DependencyInjection
{
    #region Public methods

    /// <summary>
    /// Registers application services and dependencies in the service collection.
    /// </summary>
    /// <param name="services">The service collection to register dependencies into.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        RegisterServices(services);

        return services;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Registers application service implementations.
    /// </summary>
    /// <param name="services">The service collection to register dependencies into.</param>
    private static void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IAuthService, AuthService>();
    }

    #endregion
}
