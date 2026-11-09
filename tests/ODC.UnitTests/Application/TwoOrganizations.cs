using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ODC.Web.Features;
using ODC.Web.Features.Admin.Users;
using ODC.Web.Features.Chats;
using ODC.Web.Features.News.Discussion;
using ODC.Web.Features.News.Domain;
using ODC.Web.Features.Notifications;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Infrastructure.Text;
using ODC.Web.Messaging;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Media;
using ODC.Web.Shared.Domain.Organization;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Email;
using ODC.Web.Shared.Files;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Media;
using ODC.Web.Shared.Text;

namespace ODC.UnitTests.Application;

/// <summary>
/// An in-memory cabinet with two organizations, A (with a nested subsidiary S) and B, and the services as seen by one of its users.
/// Accounts, roles and unit types live in <see cref="Master"/>; the structure, members with their positions and everything
/// the organizations do in <see cref="Db"/> (the organizations' database), as in production.
/// </summary>
internal sealed class TwoOrganizations : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TwoOrganizations"/> class.
    /// </summary>
    public TwoOrganizations()
        : this(
            new DbContextOptionsBuilder<MasterDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(static w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options,
            new DbContextOptionsBuilder<OrganizationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(static w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TwoOrganizations"/> class on a given database (PostgreSQL for the
    /// slices the in-memory provider cannot run).
    /// </summary>
    /// <param name="master">The options of the master database.</param>
    /// <param name="organization">The options of the organizations' database.</param>
    public TwoOrganizations(DbContextOptions<MasterDbContext> master, DbContextOptions<OrganizationDbContext> organization)
    {
        Db = new OrganizationDbContext(organization);

        // The organization's copies of the accounts follow the accounts, as MemberSync does in production.
        var scopes = new ServiceCollection().AddSingleton(Db).BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        Master = new MasterDbContext(new DbContextOptionsBuilder<MasterDbContext>(master).AddInterceptors(new MemberSync(scopes)).Options);

        OrganizationType = new UnitType("ORGANIZATION", "Організація", isOrganization: true);
        DivisionType = new UnitType("DIVISION", "Відділ", isOrganization: false);
        Master.UnitTypes.AddRange(OrganizationType, DivisionType);

        A = Unit("A", OrganizationType, null);
        AHr = Unit("A-HR", DivisionType, A);
        S = Unit("S", OrganizationType, A);
        SDev = Unit("S-DEV", DivisionType, S);
        B = Unit("B", OrganizationType, null);
        BProj = Unit("B-PROJ", DivisionType, B);

        Hr = Role(SystemRoles.HrSpecialist, Permissions.Users.View, Permissions.Users.Manage, Permissions.Users.Import, Permissions.Users.Export, Permissions.Users.Security, Permissions.Documents.View);
        Employee = Role(SystemRoles.Employee, Permissions.Documents.View, Permissions.News.View, Permissions.Chats.Use);
        Steward = Role("steward", Permissions.Structure.View, Permissions.Structure.Manage, Permissions.News.View, Permissions.News.Manage, Permissions.Documents.View);
        Administrator = Role(SystemRoles.Administrator);
        HelpDesk = Role("help-desk", Permissions.Tickets.Manage, Permissions.Documents.View);

        // The HR specialist of A is also an ordinary employee of B: users.* must apply only in A.
        HrA = Person("hr.a@example.com", (AHr, Hr, true), (BProj, Employee, false));
        EmployeeA = Person("employee.a@example.com", (AHr, Employee, true));
        EmployeeS = Person("employee.s@example.com", (SDev, Employee, true));
        EmployeeB = Person("employee.b@example.com", (BProj, Employee, true));
        Shared = Person("shared@example.com", (AHr, Employee, false), (BProj, Employee, true));
        StewardA = Person("steward.a@example.com", (AHr, Steward, true));
        Admin = Person("admin@example.com", (A, Administrator, true));
        HelpDeskB = Person("help.b@example.com", (BProj, HelpDesk, true));

        // The members first: saving the accounts copies them into the organizations' database.
        Db.SaveChanges();
        Master.SaveChanges();
    }

    /// <summary>Gets the master database (accounts, roles, unit types, requests, images).</summary>
    public MasterDbContext Master { get; }

    /// <summary>Gets the organizations' database (structure, members, documents, news, chats, notifications).</summary>
    public OrganizationDbContext Db { get; }

    /// <summary>Gets the organization type.</summary>
    public UnitType OrganizationType { get; }

    /// <summary>Gets the ordinary unit type.</summary>
    public UnitType DivisionType { get; }

    /// <summary>Gets organization A.</summary>
    public OrganizationUnit A { get; }

    /// <summary>Gets the HR division of A.</summary>
    public OrganizationUnit AHr { get; }

    /// <summary>Gets the subsidiary S nested in A.</summary>
    public OrganizationUnit S { get; }

    /// <summary>Gets a division of S.</summary>
    public OrganizationUnit SDev { get; }

    /// <summary>Gets organization B.</summary>
    public OrganizationUnit B { get; }

    /// <summary>Gets the projects division of B.</summary>
    public OrganizationUnit BProj { get; }

    /// <summary>Gets the HR role.</summary>
    public Role Hr { get; }

    /// <summary>Gets the employee role.</summary>
    public Role Employee { get; }

    /// <summary>Gets a role that manages the structure and news.</summary>
    public Role Steward { get; }

    /// <summary>Gets the system administrator role.</summary>
    public Role Administrator { get; }

    /// <summary>Gets a role that handles requests.</summary>
    public Role HelpDesk { get; }

    /// <summary>Gets the HR specialist of A (also an employee of B).</summary>
    public User HrA { get; }

    /// <summary>Gets an employee of A.</summary>
    public User EmployeeA { get; }

    /// <summary>Gets an employee of the subsidiary S.</summary>
    public User EmployeeS { get; }

    /// <summary>Gets an employee of B.</summary>
    public User EmployeeB { get; }

    /// <summary>Gets an employee of both A and B (main position in B).</summary>
    public User Shared { get; }

    /// <summary>Gets the structure and news manager of A.</summary>
    public User StewardA { get; }

    /// <summary>Gets the system administrator.</summary>
    public User Admin { get; }

    /// <summary>Gets the person who handles requests (in B).</summary>
    public User HelpDeskB { get; }

    /// <summary>Gets the image storage shared by the services.</summary>
    public MemoryMediaStorage Storage { get; } = new();

    /// <summary>
    /// Gets the private file storage of the chat attachments.
    /// </summary>
    public MemoryFileStorage Files { get; } = new();

    /// <summary>
    /// Gets the chat changes sent to the members' pages.
    /// </summary>
    public RecordedChatEvents ChatEvents { get; } = new();

    /// <summary>
    /// Gets the notifications sent to the recipients' pages.
    /// </summary>
    public RecordedNotificationEvents NotificationEvents { get; } = new();

    /// <summary>
    /// Gets the e-mails put into the outbox.
    /// </summary>
    public RecordedEmails Emails { get; } = new();

    /// <summary>Gets or sets the rows the fake spreadsheet returns.</summary>
    public IReadOnlyList<UserSheetRow> SheetRows { get; set; } = [];

    /// <summary>Gets or sets the rows last written by the fake spreadsheet, with the format.</summary>
    public (IReadOnlyList<UserSheetRow> Rows, SpreadsheetFormat Format)? Written { get; internal set; }

    /// <inheritdoc />
    public void Dispose()
    {
        Master.Dispose();
        Db.Dispose();
    }

    /// <summary>
    /// Builds the services as seen by a user with the effective permissions of their roles.
    /// </summary>
    /// <param name="user">The current user.</param>
    /// <param name="configure">Replaces services after the defaults (e.g. a scriptable identity service).</param>
    /// <returns>The services.</returns>
    public Services As(User user, Action<IServiceCollection>? configure = null)
    {
        var permissions = Task.Run(() => new PermissionResolver(Db, new Catalog(Master)).GetEffectivePermissionsAsync(user.Id, CancellationToken.None)).GetAwaiter().GetResult();
        var services = new ServiceCollection()
            .AddLogging()
            .AddApplication();

        // The infrastructure of production (AddInfrastructure) is replaced by the shared in-memory database and fakes.
        services.AddSingleton(Master);
        services.AddSingleton(Db);
        services.AddSingleton<IDomainEventSource>(Master);
        services.AddSingleton<IDomainEventSource>(Db);
        services.AddSingleton<ICurrentUser>(new FakeCurrentUser(user.Id, permissions));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IImageProcessor, FakeImageProcessor>();
        services.AddSingleton<IMediaStorage>(Storage);
        services.AddSingleton<IFileStorage>(Files);
        services.AddSingleton<IChatEvents>(ChatEvents);
        services.AddSingleton<INotificationEvents>(NotificationEvents);
        services.AddSingleton<INewsDiscussionEvents, SilentDiscussionEvents>();
        services.AddSingleton<IEmailOutbox>(Emails);
        services.AddSingleton<IIdentityService>(new FakeIdentity(Master));
        services.AddSingleton<IUserSpreadsheet>(new FakeSpreadsheet(this));
        services.AddSingleton<IAuditLogger, FakeAudit>();
        services.AddSingleton<IMarkdownRenderer, FakeMarkdown>();
        services.AddSingleton<IRichTextSanitizer, RichTextSanitizer>();
        services.AddSingleton(Options.Create(new EmailOptions { SiteUrl = "https://odc.test/" }));
        configure?.Invoke(services);

        var provider = services.BuildServiceProvider();
        return new Services(provider);
    }

    /// <summary>
    /// The current positions of a person (the organizations' database).
    /// </summary>
    /// <param name="user">The person.</param>
    /// <returns>The positions.</returns>
    public List<StaffPosition> PositionsOf(User user) => [.. Db.StaffPositions.Where(p => p.UserId == user.Id)];

    /// <summary>
    /// Adds a news article.
    /// </summary>
    /// <param name="title">The title.</param>
    /// <param name="organization">The organization, or <see langword="null"/> for the whole cabinet.</param>
    /// <returns>The article.</returns>
    public NewsArticle News(string title, OrganizationUnit? organization)
    {
        var article = new NewsArticle(Guid.NewGuid().ToString("N"), title, "Анонс", "<p>Текст</p>", NewsBodyFormat.Html, NewsCategory.News, Admin.Id);
        article.AssignTo(organization?.Id);
        article.Publish(DateTimeOffset.UtcNow);
        Db.NewsArticles.Add(article);
        Db.SaveChanges();
        return article;
    }

    /// <summary>
    /// Adds a unit.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <param name="type">The type.</param>
    /// <param name="parent">The parent.</param>
    /// <returns>The unit.</returns>
    private OrganizationUnit Unit(string code, UnitType type, OrganizationUnit? parent)
    {
        var unit = new OrganizationUnit(code, $"Підрозділ {code}", type.Id, parent?.Id);
        Db.OrganizationUnits.Add(unit);
        return unit;
    }

    /// <summary>
    /// Adds a role.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <param name="permissions">The permissions.</param>
    /// <returns>The role.</returns>
    private Role Role(string code, params string[] permissions)
    {
        var role = new Role(code, code, null, isSystem: false);
        role.SetPermissions(permissions);
        Master.Roles.Add(role);
        return role;
    }

    /// <summary>
    /// Adds a user with positions.
    /// </summary>
    /// <param name="email">The e-mail.</param>
    /// <param name="positions">The positions: unit, role, main position flag.</param>
    /// <returns>The user.</returns>
    private User Person(string email, params (OrganizationUnit Unit, Role Role, bool IsPrimary)[] positions)
    {
        var user = new User(email, "Тест", email.Split('@')[0], null) { NormalizedEmail = email.ToUpperInvariant() };
        Master.Users.Add(user);
        var member = new Member(MemberProfile.Of(user));
        member.SetPositions(positions.Select(static p => (p.Unit.Id, p.Role.Id, (string?)null, p.IsPrimary)));
        Db.Members.Add(member);
        return user;
    }

    /// <summary>
    /// The services of one user, resolved from a container built like production's (handlers wrapped in the pipeline).
    /// </summary>
    /// <param name="provider">The container.</param>
    internal sealed class Services(IServiceProvider provider)
    {
        /// <summary>Gets the access scope.</summary>
        public IAccessScope Scope => provider.GetRequiredService<IAccessScope>();

        /// <summary>Gets the chat slices as this user calls them.</summary>
        public ChatClient Chats => new(this);

        /// <summary>
        /// Sends a request through its handler and the pipeline, as an endpoint would.
        /// </summary>
        /// <typeparam name="TResponse">The answer type.</typeparam>
        /// <param name="request">The request.</param>
        /// <returns>The answer.</returns>
        public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var contract = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
            var handler = provider.GetRequiredService(contract);
            return (Task<TResponse>)contract.GetMethod(nameof(IRequestHandler<,>.HandleAsync))!.Invoke(handler, [request, CancellationToken.None])!;
        }
    }

    /// <summary>
    /// An in-memory image storage.
    /// </summary>
    internal sealed class MemoryMediaStorage : IMediaStorage
    {
        /// <summary>
        /// Gets the stored images.
        /// </summary>
        public HashSet<Guid> Stored { get; } = [];

        /// <inheritdoc />
        public Task SaveAsync(Guid id, ProcessedImage image, CancellationToken cancellationToken)
        {
            Stored.Add(id);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Stream? OpenRead(Guid id, bool thumbnail) => Stored.Contains(id) ? new MemoryStream([1]) : null;

        /// <inheritdoc />
        public void Delete(Guid id) => Stored.Remove(id);
    }

    /// <summary>
    /// Keeps chat files in memory.
    /// </summary>
    internal sealed class MemoryFileStorage : IFileStorage
    {
        /// <summary>
        /// Gets the stored files by key.
        /// </summary>
        public Dictionary<string, byte[]> Stored { get; } = [];

        /// <inheritdoc />
        public async Task<StoredBlob> SaveAsync(Stream content, string category, long maxSize, CancellationToken cancellationToken)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            var key = $"{category}/{Guid.NewGuid():N}";
            Stored[key] = buffer.ToArray();
            return new StoredBlob(key, buffer.Length, "sha");
        }

        /// <inheritdoc />
        public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
            => Task.FromResult<Stream>(new MemoryStream(Stored[storageKey]));

        /// <inheritdoc />
        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
        {
            Stored.Remove(storageKey);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Records the chat changes sent to the members' pages.
    /// </summary>
    internal sealed class RecordedChatEvents : IChatEvents
    {
        /// <summary>
        /// Gets the delivered messages with their recipients.
        /// </summary>
        public List<(IReadOnlyCollection<Guid> Recipients, ChatMessageDto Message)> Added { get; } = [];

        /// <summary>
        /// Gets the chats removed for some people.
        /// </summary>
        public List<(IReadOnlyCollection<Guid> Recipients, Guid ChatId)> Removed { get; } = [];

        /// <summary>
        /// Gets the chats whose history people cleared for themselves.
        /// </summary>
        public List<(IReadOnlyCollection<Guid> Recipients, Guid ChatId)> Cleared { get; } = [];

        /// <inheritdoc />
        public void MessageAdded(IReadOnlyCollection<Guid> recipients, ChatMessageDto message) => Added.Add((recipients, message));

        /// <inheritdoc />
        public void MessageChanged(IReadOnlyCollection<Guid> recipients, ChatMessageDto message)
        {
        }

        /// <inheritdoc />
        public void ChatChanged(IReadOnlyCollection<Guid> recipients, Guid chatId)
        {
        }

        /// <inheritdoc />
        public void ChatRemoved(IReadOnlyCollection<Guid> recipients, Guid chatId) => Removed.Add((recipients, chatId));

        /// <inheritdoc />
        public void HistoryCleared(IReadOnlyCollection<Guid> recipients, Guid chatId) => Cleared.Add((recipients, chatId));

        /// <inheritdoc />
        public void Read(IReadOnlyCollection<Guid> recipients, Guid chatId, Guid userId, long sequence)
        {
        }
    }

    /// <summary>
    /// Records the e-mails instead of sending them.
    /// </summary>
    internal sealed class RecordedEmails : IEmailOutbox
    {
        /// <summary>
        /// Gets the queued e-mails.
        /// </summary>
        public List<EmailMessage> Sent { get; } = [];

        /// <inheritdoc />
        public bool IsEnabled => true;

        /// <inheritdoc />
        public void Enqueue(EmailMessage message) => Sent.Add(message);
    }

    /// <summary>
    /// Records the notifications sent to the recipients' pages.
    /// </summary>
    internal sealed class RecordedNotificationEvents : INotificationEvents
    {
        /// <summary>
        /// Gets the delivered notifications with their recipients and unread counts.
        /// </summary>
        public List<(Guid UserId, NotificationDto Notification, int UnreadCount)> Received { get; } = [];

        /// <summary>
        /// Gets the unread counts sent after reading or deleting.
        /// </summary>
        public List<(Guid UserId, int UnreadCount)> Changed { get; } = [];

        /// <inheritdoc />
        void INotificationEvents.Received(Guid userId, NotificationDto notification, int unreadCount) => Received.Add((userId, notification, unreadCount));

        /// <inheritdoc />
        void INotificationEvents.Changed(Guid userId, int unreadCount) => Changed.Add((userId, unreadCount));
    }

    /// <summary>
    /// A fixed current user.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="permissions">The effective permissions.</param>
    private sealed class FakeCurrentUser(Guid id, IReadOnlySet<string> permissions) : ICurrentUser
    {
        /// <inheritdoc />
        public bool IsAuthenticated => true;

        /// <inheritdoc />
        public Guid? Id => id;

        /// <inheritdoc />
        public string? DisplayName => "Тест";

        /// <inheritdoc />
        public string? IpAddress => null;

        /// <inheritdoc />
        public IReadOnlySet<string> Permissions => permissions;

        /// <inheritdoc />
        public bool Has(string permission) => permissions.Contains(permission);
    }

    /// <summary>
    /// Identity operations that only save the context.
    /// </summary>
    /// <param name="db">The master database.</param>
    private sealed class FakeIdentity(MasterDbContext db) : IIdentityService
    {
        /// <inheritdoc />
        public async Task<Result> CreateAsync(User user)
        {
            user.NormalizedEmail = user.Email!.ToUpperInvariant();
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return Result.Success();
        }

        /// <inheritdoc />
        public async Task<Result> UpdateAsync(User user)
        {
            await db.SaveChangesAsync();
            return Result.Success();
        }

        /// <inheritdoc />
        public Task<Result> RemovePasswordAsync(User user)
        {
            user.PasswordHash = null;
            return Task.FromResult(Result.Success());
        }

        /// <inheritdoc />
        public Task<Result> ChangePasswordAsync(User user, string currentPassword, string newPassword) => Task.FromResult(Result.Success());

        /// <inheritdoc />
        public Task<Result> ResetAuthenticatorAsync(User user) => Task.FromResult(Result.Success());

        /// <inheritdoc />
        public Task InvalidateSessionsAsync(User user) => Task.CompletedTask;

        /// <inheritdoc />
        public Task<bool> VerifyAuthenticatorCodeAsync(User user, string code) => Task.FromResult(false);

        /// <inheritdoc />
        public Task<IReadOnlyList<string>> GenerateRecoveryCodesAsync(User user) => Task.FromResult<IReadOnlyList<string>>([]);

        /// <inheritdoc />
        public Task<int> CountRecoveryCodesAsync(User user) => Task.FromResult(0);

        /// <inheritdoc />
        public Task UnlockAsync(User user) => Task.CompletedTask;
    }

    /// <summary>
    /// A spreadsheet that returns the rows set by the test.
    /// </summary>
    /// <param name="world">The test cabinet.</param>
    private sealed class FakeSpreadsheet(TwoOrganizations world) : IUserSpreadsheet
    {
        /// <inheritdoc />
        public IReadOnlyList<UserSheetRow> Read(Stream content, SpreadsheetFormat format) => world.SheetRows;

        /// <inheritdoc />
        public byte[] Write(IEnumerable<UserSheetRow> rows, SpreadsheetFormat format)
        {
            world.Written = ([.. rows], format);
            return [1, 2, 3];
        }
    }

    /// <summary>
    /// An audit logger that records nothing.
    /// </summary>
    private sealed class FakeAudit : IAuditLogger
    {
        /// <inheritdoc />
        public void Add(string action, string? entityType = null, string? entityId = null, string? details = null)
        {
        }

        /// <inheritdoc />
        public Task WriteAsync(string action, Guid? userId, string? userName, string? details, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    /// <summary>
    /// A Markdown renderer that returns the source.
    /// </summary>
    private sealed class FakeMarkdown : IMarkdownRenderer
    {
        /// <inheritdoc />
        public string ToHtml(string markdown) => markdown;
    }

    /// <summary>
    /// An image processor that accepts any non-empty file as a 100×100 image.
    /// </summary>
    private sealed class FakeImageProcessor : IImageProcessor
    {
        /// <inheritdoc />
        public Task<ProcessedImage?> ProcessAsync(ReadOnlyMemory<byte> source, MediaPurpose purpose, ImageCrop? crop, CancellationToken cancellationToken)
            => Task.FromResult(source.Length == 0 || source.Span[0] == 0 ? null : new ProcessedImage([1, 2, 3], [1], 100, 100));
    }
}
