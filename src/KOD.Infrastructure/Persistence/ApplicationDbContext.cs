using KOD.Application.Abstractions;
using KOD.Domain.Access;
using KOD.Domain.Audit;
using KOD.Domain.Common;
using KOD.Domain.Documents;
using KOD.Domain.Media;
using KOD.Domain.News;
using KOD.Domain.Organization;
using KOD.Domain.Users;
using KOD.Infrastructure.Identity.Tokens;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KOD.Infrastructure.Persistence;

/// <summary>
/// The EF Core database context (PostgreSQL) with the complete ASP.NET Core Identity schema: <c>users</c>, <c>roles</c>,
/// <c>user_roles</c> (positions: a role in an organization unit), <c>role_claims</c> (role permissions), <c>user_claims</c>
/// (individual permission adjustments), <c>user_logins</c> and <c>user_tokens</c> (authenticator key, recovery codes).
/// </summary>
/// <param name="options">The context options.</param>
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<User, Role, Guid, UserPermissionOverride, StaffPosition, IdentityUserLogin<Guid>, RolePermission, IdentityUserToken<Guid>>(options),
        IApplicationDbContext,
        IDataProtectionKeyContext
{
    /// <summary>
    /// The name of the sequence that issues document registration numbers.
    /// </summary>
    public const string DocumentNumberSequence = "document_number_seq";

    /// <inheritdoc />
    public DbSet<StaffPosition> StaffPositions => UserRoles;

    /// <inheritdoc />
    public DbSet<UserPermissionOverride> UserPermissionOverrides => UserClaims;

    /// <inheritdoc />
    public DbSet<OrganizationUnit> OrganizationUnits => Set<OrganizationUnit>();

    /// <inheritdoc />
    public DbSet<UnitType> UnitTypes => Set<UnitType>();

    /// <inheritdoc />
    public DbSet<Document> Documents => Set<Document>();

    /// <summary>
    /// Gets the signing route steps. Not used by the application (they are loaded through documents), but the set keeps
    /// the entity's discovery order, and with it the generated constraint names, stable.
    /// </summary>
    public DbSet<DocumentSigner> DocumentSigners => Set<DocumentSigner>();

    /// <inheritdoc />
    public DbSet<DocumentSignature> DocumentSignatures => Set<DocumentSignature>();

    /// <inheritdoc />
    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();

    /// <inheritdoc />
    public DbSet<NewsComment> NewsComments => Set<NewsComment>();

    /// <inheritdoc />
    public DbSet<NewsReaction> NewsReactions => Set<NewsReaction>();

    /// <inheritdoc />
    public DbSet<NewsView> NewsViews => Set<NewsView>();

    /// <inheritdoc />
    public DbSet<MediaFile> MediaFiles => Set<MediaFile>();

    /// <inheritdoc />
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    /// <inheritdoc />
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    /// <summary>
    /// Gets the refresh token families: one per sign-in, the only server-side state of the JWT authentication.
    /// </summary>
    public DbSet<RefreshTokenFamily> RefreshTokenFamilies => Set<RefreshTokenFamily>();

    /// <inheritdoc />
    public void DiscardChanges() => ChangeTracker.Clear();

    /// <inheritdoc />
    public Task<T> InTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var strategy = Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(
            (Context: this, Operation: operation),
            static async (_, state, ct) =>
            {
                await using var transaction = await state.Context.Database.BeginTransactionAsync(ct);
                var result = await state.Operation(ct);
                await transaction.CommitAsync(ct);
                return result;
            },
            verifySucceeded: null,
            cancellationToken);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasPostgresExtension("citext");
        builder.HasSequence<long>(DocumentNumberSequence);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Constraint names were derived from the default Identity table name (AspNetUsers) before it was renamed.
        foreach (var foreignKey in builder.Model.GetEntityTypes().SelectMany(static t => t.GetForeignKeys()))
        {
            if (foreignKey.GetConstraintName() is { } name && name.Contains("_asp_net_users_", StringComparison.Ordinal))
            {
                foreignKey.SetConstraintName(name.Replace("_asp_net_users_", "_users_", StringComparison.Ordinal));
            }
        }

        // Keys are UUID v7 assigned by the domain; new entities reachable through navigations must be inserted, not updated.
        Type[] identityEntities = [typeof(User), typeof(Role), typeof(StaffPosition)];
        foreach (var entityType in builder.Model.GetEntityTypes().Where(t => typeof(Entity).IsAssignableFrom(t.ClrType) || identityEntities.Contains(t.ClrType)))
        {
            builder.Entity(entityType.ClrType).Property(nameof(Entity.Id)).ValueGeneratedNever();
        }
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        // Enums are stored as readable strings.
        configurationBuilder.Properties<DocumentKind>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<DocumentStatus>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<SigningMode>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<SignerStatus>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<SignatureFormat>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<SignaturePackaging>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<KeyMediaType>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<NewsCategory>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<NewsBodyFormat>().HaveConversion<string>().HaveMaxLength(16);
        configurationBuilder.Properties<ReactionKind>().HaveConversion<string>().HaveMaxLength(16);
        configurationBuilder.Properties<MediaPurpose>().HaveConversion<string>().HaveMaxLength(16);
    }
}
