using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Features.Auth.Activation;
using ODC.Web.Features.News.Domain;
using ODC.Web.Features.Tickets.Domain;
using ODC.Web.Infrastructure.Files;
using ODC.Web.Infrastructure.Identity.Tokens;
using ODC.Web.Messaging;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Domain.Media;
using ODC.Web.Shared.Domain.Organization;
using ODC.Web.Shared.Domain.Tenancy;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Infrastructure.Persistence;

/// <summary>
/// The master database (PostgreSQL): the accounts with the ASP.NET Core Identity user tables (<c>users</c>,
/// <c>user_claims</c> — unused, <c>user_logins</c>, <c>user_tokens</c>: authenticator key, recovery codes), the sign-in
/// state, the catalogs every organization shares (roles and their permissions, unit types), images, requests to the
/// platform support, the platform's audit trail and the Data Protection keys. Positions, the structure and everything an
/// organization does live in its own database (<see cref="OrganizationDbContext"/>; see <see cref="DataPlacement"/>).
/// </summary>
/// <param name="options">The context options.</param>
public sealed class MasterDbContext(DbContextOptions<MasterDbContext> options)
    : IdentityUserContext<User, Guid, IdentityUserClaim<Guid>, IdentityUserLogin<Guid>, IdentityUserToken<Guid>>(options),
        IDataProtectionKeyContext,
        IDomainEventSource
{
    /// <summary>
    /// Gets the registry of organizations (where each one's database is).
    /// </summary>
    public DbSet<Organization> Organizations => Set<Organization>();

    /// <summary>
    /// Gets the memberships of accounts in organizations.
    /// </summary>
    public DbSet<Membership> Memberships => Set<Membership>();

    /// <summary>
    /// Gets the participation of accounts in organizations they do not work in (guests).
    /// </summary>
    public DbSet<Participation> Participations => Set<Participation>();

    /// <summary>
    /// Gets the index of the news of the whole cabinet.
    /// </summary>
    public DbSet<GeneralArticle> GeneralArticles => Set<GeneralArticle>();

    /// <summary>
    /// Gets the roles.
    /// </summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <summary>
    /// Gets the permissions of roles.
    /// </summary>
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    /// <summary>
    /// Gets the unit types.
    /// </summary>
    public DbSet<UnitType> UnitTypes => Set<UnitType>();

    /// <summary>
    /// Gets the uploaded images.
    /// </summary>
    public DbSet<MediaFile> MediaFiles => Set<MediaFile>();

    /// <summary>
    /// Gets the requests of users.
    /// </summary>
    public DbSet<Ticket> Tickets => Set<Ticket>();

    /// <summary>
    /// Gets the messages and notes of requests.
    /// </summary>
    public DbSet<TicketMessage> TicketMessages => Set<TicketMessage>();

    /// <summary>
    /// Gets the files of request messages.
    /// </summary>
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();

    /// <summary>
    /// Gets the audit trail of the platform (sign-in, accounts, roles, catalogs, requests).
    /// </summary>
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    /// <inheritdoc />
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    /// <summary>
    /// Gets the refresh token families: one per sign-in, the only server-side state of the JWT authentication.
    /// </summary>
    public DbSet<RefreshTokenFamily> RefreshTokenFamilies => Set<RefreshTokenFamily>();

    /// <summary>
    /// Gets the requests of activation codes (accounts that wait for their owner to choose the first password).
    /// </summary>
    public DbSet<ActivationChallenge> ActivationChallenges => Set<ActivationChallenge>();

    /// <summary>
    /// Gets the secrets of the platform (the key of its files), encrypted with Data Protection.
    /// </summary>
    public DbSet<PlatformSecret> PlatformSecrets => Set<PlatformSecret>();

    /// <summary>
    /// Stops tracking every loaded entity and discards their pending changes (e.g. to reload after a concurrency conflict).
    /// </summary>
    public void DiscardChanges() => ChangeTracker.Clear();

    /// <summary>
    /// Takes the domain events of the tracked entities (delivered by the request pipeline after the request succeeded).
    /// </summary>
    /// <returns>The events.</returns>
    public IReadOnlyList<IDomainEvent> TakeDomainEvents()
        => [.. ChangeTracker.Entries<Entity>().SelectMany(static e => e.Entity.TakeDomainEvents())];

    /// <summary>
    /// Runs <paramref name="operation"/> inside a database transaction using the configured execution strategy.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="operation">The operation to run.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>
    public Task<T> InTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
        => ModelRules.InTransactionAsync(this, operation, cancellationToken);

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        ModelRules.ApplyConfigurations(builder, DataPlacement.Master);

        // Constraint names were derived from the default Identity table name (AspNetUsers) before it was renamed.
        foreach (var foreignKey in builder.Model.GetEntityTypes().SelectMany(static t => t.GetForeignKeys()))
        {
            if (foreignKey.GetConstraintName() is { } name && name.Contains("_asp_net_users_", StringComparison.Ordinal))
            {
                foreignKey.SetConstraintName(name.Replace("_asp_net_users_", "_users_", StringComparison.Ordinal));
            }
        }

        ModelRules.KeysAssignedByDomain(builder, typeof(User), typeof(Role));
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        => ModelRules.EnumsAsStrings(configurationBuilder);
}
