using Microsoft.EntityFrameworkCore;
using ODC.Web.Features.Chats.Domain;
using ODC.Web.Features.Documents.Domain;
using ODC.Web.Features.News.Domain;
using ODC.Web.Features.Notifications.Domain;
using ODC.Web.Messaging;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Domain.Organization;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Infrastructure.Persistence;

/// <summary>
/// The database of an organization (PostgreSQL): its structure, its members (copies of the accounts' profiles) with
/// their positions and individual permissions, documents and signatures, news with discussions, chats, notifications
/// and the organization's audit trail. Accounts and the shared catalogs live in the master database
/// (<see cref="MasterDbContext"/>; see <see cref="DataPlacement"/>), so this context never joins them.
/// </summary>
/// <remarks>Until there is a database per organization (phase 3) every organization shares one.</remarks>
/// <param name="options">The context options.</param>
public sealed class OrganizationDbContext(DbContextOptions<OrganizationDbContext> options) : DbContext(options), IDomainEventSource
{
    /// <summary>
    /// The name of the sequence that issues document registration numbers.
    /// </summary>
    public const string DocumentNumberSequence = "document_number_seq";

    /// <summary>
    /// Gets the people the organization knows (copies of the accounts' profiles).
    /// </summary>
    public DbSet<Member> Members => Set<Member>();

    /// <summary>
    /// Gets the positions held by members.
    /// </summary>
    public DbSet<StaffPosition> StaffPositions => Set<StaffPosition>();

    /// <summary>
    /// Gets the individual permission adjustments.
    /// </summary>
    public DbSet<UserPermissionOverride> UserPermissionOverrides => Set<UserPermissionOverride>();

    /// <summary>
    /// Gets the organization units.
    /// </summary>
    public DbSet<OrganizationUnit> OrganizationUnits => Set<OrganizationUnit>();

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
    /// Gets the notifications of members.
    /// </summary>
    public DbSet<Notification> Notifications => Set<Notification>();

    /// <summary>
    /// Gets the organization's audit trail.
    /// </summary>
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

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
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasSequence<long>(DocumentNumberSequence);
        ModelRules.ApplyConfigurations(modelBuilder, DataPlacement.Organization);
        ModelRules.KeysAssignedByDomain(modelBuilder, typeof(Member), typeof(StaffPosition), typeof(UserPermissionOverride));
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        => ModelRules.EnumsAsStrings(configurationBuilder);
}
