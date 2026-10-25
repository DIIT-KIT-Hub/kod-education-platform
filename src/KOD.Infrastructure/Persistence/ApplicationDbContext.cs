using KOD.Application.Abstractions;
using KOD.Domain.Access;
using KOD.Domain.Audit;
using KOD.Domain.Common;
using KOD.Domain.Documents;
using KOD.Domain.News;
using KOD.Domain.Organization;
using KOD.Domain.Sessions;
using KOD.Domain.Users;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KOD.Infrastructure.Persistence;

/// <summary>
/// The EF Core database context (PostgreSQL). Identity provides only the user store; roles and permissions are domain entities.
/// </summary>
/// <param name="options">The context options.</param>
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityUserContext<User, Guid>(options), IApplicationDbContext, IDataProtectionKeyContext
{
    /// <summary>
    /// The name of the sequence that issues document registration numbers.
    /// </summary>
    public const string DocumentNumberSequence = "document_number_seq";

    /// <inheritdoc />
    public DbSet<StaffPosition> StaffPositions => Set<StaffPosition>();

    /// <inheritdoc />
    public DbSet<UserPermissionOverride> UserPermissionOverrides => Set<UserPermissionOverride>();

    /// <inheritdoc />
    public DbSet<Role> Roles => Set<Role>();

    /// <inheritdoc />
    public DbSet<OrganizationUnit> OrganizationUnits => Set<OrganizationUnit>();

    /// <inheritdoc />
    public DbSet<UnitType> UnitTypes => Set<UnitType>();

    /// <inheritdoc />
    public DbSet<Document> Documents => Set<Document>();

    /// <inheritdoc />
    public DbSet<DocumentSigner> DocumentSigners => Set<DocumentSigner>();

    /// <inheritdoc />
    public DbSet<DocumentSignature> DocumentSignatures => Set<DocumentSignature>();

    /// <inheritdoc />
    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();

    /// <inheritdoc />
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    /// <inheritdoc />
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    /// <inheritdoc />
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

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

        // Keys are UUID v7 assigned by the domain; new entities reachable through navigations must be inserted, not updated.
        foreach (var entityType in builder.Model.GetEntityTypes().Where(static t => typeof(Entity).IsAssignableFrom(t.ClrType) || t.ClrType == typeof(User)))
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
    }
}
