using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Features.Admin.Users;
using ODC.Web.Features.Auth.Activation;
using ODC.Web.Features.Documents;
using ODC.Web.Infrastructure.Auditing;
using ODC.Web.Infrastructure.Email;
using ODC.Web.Infrastructure.Files;
using ODC.Web.Infrastructure.Identity;
using ODC.Web.Infrastructure.Identity.Tokens;
using ODC.Web.Infrastructure.Media;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Infrastructure.Persistence.Seeding;
using ODC.Web.Infrastructure.Signatures;
using ODC.Web.Infrastructure.Tenancy;
using ODC.Web.Infrastructure.Text;
using ODC.Web.Messaging;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Email;
using ODC.Web.Shared.Files;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Media;
using ODC.Web.Shared.Text;

namespace ODC.Web.Infrastructure;

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
            services.AddMemoryCache();
            services.AddSingleton(new OrganizationDatabases(connectionString));
            services.AddSingleton<OrganizationRegistry>();
            services.AddSingleton<OrganizationScopes>();
            services.AddSingleton<AccessCache>();
            services.AddSingleton<MemberCopies>();
            services.AddSingleton<IAccountPermissions, AccountPermissions>();
            services.AddScoped<OrganizationContext>();
            services.AddScoped<Memberships>();
            services.AddScoped<ExternalParticipants>();
            services.AddScoped<Workplaces>();
            services.AddScoped<IAcrossOrganizations, AcrossOrganizations>();
            services.AddScoped<IOrganizationProvisioning, OrganizationProvisioning>();
            services.AddSingleton<AuditableEntityInterceptor>();
            services.AddScoped<PendingAudit>();
            services.AddScoped<PendingAuditInterceptor>();
            services.AddScoped(static provider => new MemberSync(provider.GetRequiredService<MemberCopies>().CopyAsync));
            services.AddDbContext<MasterDbContext>((provider, options) => options
                .UseNpgsql(connectionString, npgsql => npgsql
                    .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
                    .EnableRetryOnFailure(maxRetryCount: 3))
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(
                    provider.GetRequiredService<AuditableEntityInterceptor>(),
                    provider.GetRequiredService<PendingAuditInterceptor>(),
                    provider.GetRequiredService<MemberSync>()));

            // The database of the request's organization: none chosen, none opened (NoOrganizationException → 409). The EF
            // Core tools have no request; migrations never connect.
            services.AddDbContext<OrganizationDbContext>((provider, options) => options
                .UseNpgsql(EF.IsDesignTime ? connectionString : provider.GetRequiredService<OrganizationDatabases>().ConnectionStringOf(provider.GetRequiredService<OrganizationContext>().Require()), npgsql => npgsql
                    .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
                    .EnableRetryOnFailure(maxRetryCount: 3))
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(
                    provider.GetRequiredService<AuditableEntityInterceptor>(),
                    provider.GetRequiredService<PendingAuditInterceptor>()));
            services.AddScoped<IDomainEventSource>(static provider => provider.GetRequiredService<MasterDbContext>());
            services.AddScoped<IDomainEventSource>(static provider => new OrganizationEvents(provider));

            services.AddIdentityServices();
            services.AddKeyProtection(configuration, environment);

            services.Configure<PasswordSecurityOptions>(configuration.GetSection(PasswordSecurityOptions.SectionName));
            services.AddHttpClient(PwnedPasswordValidator.HttpClientName, static client =>
            {
                client.Timeout = TimeSpan.FromSeconds(5);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("ODC-organizational-documents");
            });

            services.Configure<SessionLifetimeOptions>(configuration.GetSection(SessionLifetimeOptions.SectionName));
            services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
            services.Configure<ActivationOptions>(configuration.GetSection(ActivationOptions.SectionName));
            services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
            services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));
            services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

            services.AddScoped<IDocumentNumberGenerator, DocumentNumberGenerator>();
            services.AddScoped<IAuditLogger, AuditLogger>();
            services.AddScoped<DatabaseInitializer>();
            services.AddSingleton<IFileStorage, LocalFileStorage>();
            services.AddSingleton<IUserSpreadsheet, UserSpreadsheet>();
            services.AddSingleton<IMarkdownRenderer, MarkdigRenderer>();
            services.AddSingleton<IRichTextSanitizer, RichTextSanitizer>();
            services.AddSingleton<IImageProcessor, VipsImageProcessor>();
            services.AddSingleton<IMediaStorage, LocalMediaStorage>();
            services.AddHostedService<MediaCleanupService>();
            services.AddSingleton<EmailOutbox>();
            services.AddSingleton<IEmailOutbox>(static provider => provider.GetRequiredService<EmailOutbox>());
            services.AddHostedService<SmtpEmailSender>();
            services.AddSingleton<ISignatureInspector, CmsSignatureInspector>();

            return services;
        }

        /// <summary>
        /// Adds ASP.NET Core Identity (user and role stores, sign-in manager, token providers) and the token services: the
        /// ES256 signing key, the JWT access token service and the store of rotating refresh token families.
        /// </summary>
        /// <returns>The service collection.</returns>
        private IServiceCollection AddIdentityServices()
        {
            services.AddIdentityCore<User>(static options =>
                {
                    // Registered JWT claim names keep the access tokens compact (RFC 7519 §4.1, OpenID Connect).
                    options.ClaimsIdentity.UserIdClaimType = AppClaimTypes.Subject;
                    options.ClaimsIdentity.UserNameClaimType = AppClaimTypes.Name;
                    options.ClaimsIdentity.EmailClaimType = AppClaimTypes.Email;
                    options.ClaimsIdentity.RoleClaimType = AppClaimTypes.Role;

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
                .AddEntityFrameworkStores<MasterDbContext>()

                // The ASP.NET managers pass the request's cancellation token to the stores (the plain ones of
                // AddIdentityCore pass none), so Identity queries stop when the client gives up.
                .AddUserManager<AspNetUserManager<User>>()
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
            services.AddSingleton<JwtSigningKey>();
            services.AddSingleton<JwtTokenService>();
            services.AddSingleton<RefreshTokenStore>();
            services.AddScoped<AccountActivationService>();
            services.AddHostedService<RefreshTokenCleanupService>();

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
                .SetApplicationName("ODC")
                .PersistKeysToDbContext<MasterDbContext>();

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
