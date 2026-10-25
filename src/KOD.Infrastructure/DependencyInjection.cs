using System.Security.Cryptography.X509Certificates;
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
using Microsoft.Extensions.Hosting;

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
        /// <param name="environment">The host environment.</param>
        /// <returns>The service collection.</returns>
        public IServiceCollection AddInfrastructure(IConfiguration configuration, IHostEnvironment environment)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(environment);

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
            services.AddKeyProtection(configuration, environment);

            services.Configure<PasswordSecurityOptions>(configuration.GetSection(PasswordSecurityOptions.SectionName));
            services.AddHttpClient(PwnedPasswordValidator.HttpClientName, static client =>
            {
                client.Timeout = TimeSpan.FromSeconds(5);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("KOD-organizational-documents");
            });

            services.Configure<SessionLifetimeOptions>(configuration.GetSection(SessionLifetimeOptions.SectionName));
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
                .AddUserStore<ProtectedUserStore>()
                .AddSignInManager<AppSignInManager>()
                .AddClaimsPrincipalFactory<AppUserClaimsPrincipalFactory>()
                .AddErrorDescriber<UkrainianIdentityErrorDescriber>()
                .AddPasswordValidator<MaximumLengthPasswordValidator>()
                .AddPasswordValidator<PwnedPasswordValidator>()
                .AddDefaultTokenProviders()
                .AddTokenProvider<ReplayProtectedAuthenticatorTokenProvider>(TokenOptions.DefaultAuthenticatorProvider);

            services.AddSingleton<UserTokenProtector>();
            services.AddHttpContextAccessor();
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddSingleton<DatabaseTicketStore>();
            services.AddSingleton<ITicketStore>(static sp => sp.GetRequiredService<DatabaseTicketStore>());
            services.AddSingleton<IUserSessionValidator>(static sp => sp.GetRequiredService<DatabaseTicketStore>());
            services.AddOptions<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme)
                .Configure<ITicketStore>(static (options, store) => options.SessionStore = store);
            services.AddHostedService<SessionCleanupService>();

            return services;
        }

        /// <summary>
        /// Stores Data Protection keys in the database encrypted at rest: with the X.509 certificate from
        /// <c>DataProtection:CertificatePath</c> (any OS, required in production outside Windows) or with Windows DPAPI.
        /// A database dump alone is then not enough to decrypt sessions or forge cookies.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="environment">The host environment.</param>
        /// <returns>The service collection.</returns>
        private IServiceCollection AddKeyProtection(IConfiguration configuration, IHostEnvironment environment)
        {
            var builder = services.AddDataProtection()
                .SetApplicationName("KOD")
                .PersistKeysToDbContext<ApplicationDbContext>();

            var certificatePath = configuration["DataProtection:CertificatePath"];
            if (!string.IsNullOrWhiteSpace(certificatePath))
            {
                var certificate = X509CertificateLoader.LoadPkcs12FromFile(
                    Path.GetFullPath(certificatePath, environment.ContentRootPath),
                    configuration["DataProtection:CertificatePassword"],
                    X509KeyStorageFlags.EphemeralKeySet);
                builder.ProtectKeysWithCertificate(certificate).UnprotectKeysWithAnyCertificate(certificate);
            }
            else if (OperatingSystem.IsWindows())
            {
                builder.ProtectKeysWithDpapi();
            }
            else if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "Data Protection keys must be encrypted: set DataProtection:CertificatePath (a PFX file) and DataProtection:CertificatePassword.");
            }

            return services;
        }
    }
}
