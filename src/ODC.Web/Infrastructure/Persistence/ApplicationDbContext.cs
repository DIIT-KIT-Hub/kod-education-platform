using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Features.Auth.Activation;
using ODC.Web.Features.Chats.Domain;
using ODC.Web.Features.Documents.Domain;
using ODC.Web.Features.News.Domain;
using ODC.Web.Features.Notifications.Domain;
using ODC.Web.Features.Tickets.Domain;
using ODC.Web.Infrastructure.Identity.Tokens;
using ODC.Web.Messaging;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Domain.Media;
using ODC.Web.Shared.Domain.Organization;
using ODC.Web.Shared.Domain.Reactions;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Infrastructure.Persistence;

/// <summary>
/// The EF Core database context (PostgreSQL) with the complete ASP.NET Core Identity schema: <c>users</c>, <c>roles</c>,
/// <c>user_roles</c> (positions: a role in an organization unit), <c>role_claims</c> (role permissions), <c>user_claims</c>
/// (individual permission adjustments), <c>user_logins</c> and <c>user_tokens</c> (authenticator key, recovery codes).
/// </summary>
/// <param name="options">The context options.</param>
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<User, Role, Guid, UserPermissionOverride, StaffPosition, IdentityUserLogin<Guid>, RolePermission, IdentityUserToken<Guid>>(options),
        IDataProtectionKeyContext,
        IDomainEventSource
{
    /// <summary>
    /// The name of the sequence that issues document registration numbers.
    /// </summary>
    public const string DocumentNumberSequence = "document_number_seq";

    /// <summary>
    /// Gets the positions held by users.
    /// </summary>
    public DbSet<StaffPosition> StaffPositions => UserRoles;

    /// <summary>
    /// Gets the individual permission adjustments.
    /// </summary>
    public DbSet<UserPermissionOverride> UserPermissionOverrides => UserClaims;

    /// <summary>
    /// Gets the organization units.
    /// </summary>
    public DbSet<OrganizationUnit> OrganizationUnits => Set<OrganizationUnit>();

    /// <summary>
    /// Gets the unit types.
    /// </summary>
    public DbSet<UnitType> UnitTypes => Set<UnitType>();

    /// <summary>
    /// Gets the documents.
    /// </summary>
    public DbSet<Document> Documents => Set<Document>();

    /// <summary>
    /// Gets the signing route steps. Not used by the application (they are loaded through documents), but the set keeps
    /// the entity's discovery order, and with it the generated constraint names, stable.
    /// </summary>
    public DbSet<DocumentSigner> DocumentSigners => Set<DocumentSigner>();

    /// <summary>
    /// Gets the document signatures.
    /// </summary>
    public DbSet<DocumentSignature> DocumentSignatures => Set<DocumentSignature>();

    /// <summary>
    /// Gets the news articles.
    /// </summary>
    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();

    /// <summary>
    /// Gets the comments on news articles.
    /// </summary>
    public DbSet<NewsComment> NewsComments => Set<NewsComment>();

    /// <summary>
    /// Gets the reactions to news articles and their comments.
    /// </summary>
    public DbSet<NewsReaction> NewsReactions => Set<NewsReaction>();

    /// <summary>
    /// Gets the views of news articles (one per reader).
    /// </summary>
    public DbSet<NewsView> NewsViews => Set<NewsView>();

    /// <summary>
    /// Gets the uploaded images.
    /// </summary>
    public DbSet<MediaFile> MediaFiles => Set<MediaFile>();

    /// <summary>
    /// Gets the chats.
    /// </summary>
    public DbSet<Chat> Chats => Set<Chat>();

    /// <summary>
    /// Gets the members of chats.
    /// </summary>
    public DbSet<ChatMember> ChatMembers => Set<ChatMember>();

    /// <summary>
    /// Gets the chat messages.
    /// </summary>
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    /// <summary>
    /// Gets the files and photos of chat messages.
    /// </summary>
    public DbSet<ChatAttachment> ChatAttachments => Set<ChatAttachment>();

    /// <summary>
    /// Gets the reactions to chat messages.
    /// </summary>
    public DbSet<ChatReaction> ChatReactions => Set<ChatReaction>();

    /// <summary>
    /// Gets the chat messages members deleted only for themselves.
    /// </summary>
    public DbSet<HiddenChatMessage> HiddenChatMessages => Set<HiddenChatMessage>();

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
    /// Gets the notifications of users.
    /// </summary>
    public DbSet<Notification> Notifications => Set<Notification>();

    /// <summary>
    /// Gets the audit trail.
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
        configurationBuilder.Properties<TicketCategory>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<TicketPriority>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<TicketStatus>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<TicketMessageKind>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<NotificationKind>().HaveConversion<string>().HaveMaxLength(32);
    }
}
