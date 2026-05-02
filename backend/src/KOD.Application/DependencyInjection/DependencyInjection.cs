using KOD.Application.Abstractions.Services.Auth;
using KOD.Application.Abstractions.Services.Identity;
using KOD.Application.Abstractions.Services.Otp;

using Microsoft.Extensions.DependencyInjection;

namespace KOD.Application.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        RegisterServices(services);

        return services;
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IAuthService, AuthService>();
    }

}
