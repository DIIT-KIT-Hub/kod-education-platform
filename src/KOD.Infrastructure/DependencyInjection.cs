using KOD.Application.Abstractions;
using KOD.Application.Features.Users;
using KOD.Domain.Users;
using KOD.Infrastructure.Auditing;
using KOD.Infrastructure.Files;
using KOD.Infrastructure.Identity;
using KOD.Infrastructure.Persistence;
using KOD.Infrastructure.Persistence.Seeding;
using KOD.Infrastructure.Signatures;
using KOD.Infrastructure.Spreadsheets;
using KOD.Infrastructure.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KOD.Infrastructure;

/// <summary>
/// Registers persistence, ASP.NET Core Identity and other infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the infrastructure layer.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <returns>The service collection.</returns>
        public IServiceCollection AddInfrastructure(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            var connectionString = configuration.GetConnectionString("Database")
                ?? throw new InvalidOperationException("Connection string 'ConnectionStrings:Database' is not configured.");

            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<AuditableEntityInterceptor>();
            services.AddDbContext<ApplicationDbContext>((provider, options) => options
                .UseNpgsql(connectionString, npgsql => npgsql
                    .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
                    .EnableRetryOnFailure(maxRetryCount: 3))
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(provider.GetRequiredService<AuditableEntityInterceptor>()));
            services.AddScoped<IApplicationDbContext>(static provider => provider.GetRequiredService<ApplicationDbContext>());

            services.AddIdentityServices();

            services.AddDataProtection()
                .SetApplicationName("KOD")
                .PersistKeysToDbContext<ApplicationDbContext>();

            services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
            services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));

            services.AddScoped<IDocumentNumberGenerator, DocumentNumberGenerator>();
            services.AddScoped<IAuditLogger, AuditLogger>();
            services.AddScoped<DatabaseInitializer>();
            services.AddSingleton<IFileStorage, LocalFileStorage>();
            services.AddSingleton<IPublicAssetStorage, PublicAssetStorage>();
            services.AddSingleton<IUserSpreadsheet, UserSpreadsheet>();
            services.AddSingleton<IMarkdownRenderer, MarkdigRenderer>();
            services.AddSingleton<ISignatureInspector, CmsSignatureInspector>();

            return services;
        }

        /// <summary>
        /// Adds ASP.NET Core Identity (user store, sign-in manager, token providers) and the server-side ticket store
        /// for the Identity application cookie.
        /// </summary>
        /// <returns>The service collection.</returns>
        private IServiceCollection AddIdentityServices()
        {
            services.AddIdentityCore<User>(static options =>
                {
                    options.User.RequireUniqueEmail = true;
                    options.SignIn.RequireConfirmedAccount = false;

                    options.Password.RequiredLength = 12;
                    options.Password.RequiredUniqueChars = 5;
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = false;

                    options.Lockout.AllowedForNewUsers = true;
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddSignInManager<AppSignInManager>()
                .AddClaimsPrincipalFactory<AppUserClaimsPrincipalFactory>()
                .AddErrorDescriber<UkrainianIdentityErrorDescriber>()
                .AddDefaultTokenProviders();

            services.AddHttpContextAccessor();
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddSingleton<ITicketStore, DatabaseTicketStore>();
            services.AddOptions<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme)
                .Configure<ITicketStore>(static (options, store) => options.SessionStore = store);
            services.AddHostedService<SessionCleanupService>();

            return services;
        }
    }
}
