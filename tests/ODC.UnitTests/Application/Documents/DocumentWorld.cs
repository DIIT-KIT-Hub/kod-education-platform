using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ODC.Web.Features;
using ODC.Web.Features.Chats;
using ODC.Web.Features.Documents;
using ODC.Web.Features.Documents.Domain;
using ODC.Web.Features.News.Discussion;
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
using ODC.Web.Shared.Media;
using ODC.Web.Shared.Text;

namespace ODC.UnitTests.Application.Documents;

/// <summary>
/// The <see cref="TwoOrganizations"/> cabinet extended for the document slices: people who author, sign, view and manage
/// documents, a manual clock, a content-addressed file storage with real SHA-256 digests, a scripted signature inspector,
/// a recorded audit log and a counter for registration numbers.
/// </summary>
internal sealed class DocumentWorld : IDisposable
{
    /// <summary>
    /// The moment the clock starts at (a Wednesday in October, Kyiv time 13:00).
    /// </summary>
    public static readonly DateTimeOffset Start = new(2026, 10, 21, 10, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// The services built for each user.
    /// </summary>
    private readonly Dictionary<Guid, Session> _sessions = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentWorld"/> class.
    /// </summary>
    /// <param name="master">The options of the master database (a PostgreSQL test database).</param>
    /// <param name="organization">The options of the organizations' database (a PostgreSQL test database).</param>
    public DocumentWorld(DbContextOptions<MasterDbContext> master, DbContextOptions<OrganizationDbContext> organization)
    {
        World = new TwoOrganizations(master, organization);
        Clerk = Role("clerk", Permissions.Documents.View, Permissions.Documents.Create, Permissions.Documents.Sign);
        Registrar = Role("registrar", Permissions.Documents.View, Permissions.Documents.ViewOrganization, Permissions.Documents.Create, Permissions.Documents.Sign, Permissions.Documents.Manage);
        UnitViewer = Role("unit-viewer", Permissions.Documents.View, Permissions.Documents.ViewUnit);
        Auditor = Role("auditor", Permissions.Documents.View, Permissions.Documents.ViewAll);
        Reader = Role("reader", Permissions.Documents.View);
        Outsider = Role("outsider", Permissions.News.View);
        Tool = Role("tool", Permissions.Signing.Use);

        AuthorA = Person("author.a@example.com", "Автор", "Анна", World.AHr, Clerk, save: false);
        SignerA = Person("signer.a@example.com", "Бондар", "Богдан", World.AHr, Clerk, save: false);
        SignerA2 = Person("signer.a2@example.com", "Василенко", "Віра", World.A, Clerk, save: false);
        SignerS = Person("signer.s@example.com", "Гончар", "Галина", World.SDev, Clerk, save: false);
        SignerB = Person("signer.b@example.com", "Давиденко", "Денис", World.BProj, Clerk, save: false);
        RegistrarA = Person("registrar.a@example.com", "Єременко", "Євген", World.A, Registrar, save: false);
        UnitViewerHr = Person("viewer.hr@example.com", "Жук", "Жанна", World.AHr, UnitViewer, save: false);
        AuditorB = Person("auditor.b@example.com", "Зайченко", "Зоя", World.BProj, Auditor, save: false);
        ReaderA = Person("reader.a@example.com", "Іваненко", "Ірина", World.AHr, Reader, save: false);
        OutsiderA = Person("outsider.a@example.com", "Коваль", "Кирило", World.AHr, Outsider, save: false);
        ToolUser = Person("tool@example.com", "Литвин", "Леся", World.AHr, Tool, save: false);
        Db.SaveChanges();
        World.Master.SaveChanges();
    }

    /// <summary>Gets the underlying cabinet (organizations A with subsidiary S, and B).</summary>
    public TwoOrganizations World { get; }

    /// <summary>Gets the shared organizations' database.</summary>
    public OrganizationDbContext Db => World.Db;

    /// <summary>Gets the role that views, creates and signs documents.</summary>
    public Role Clerk { get; }

    /// <summary>Gets the role that sees the documents of its organization and manages them.</summary>
    public Role Registrar { get; }

    /// <summary>Gets the role that sees the documents of its units.</summary>
    public Role UnitViewer { get; }

    /// <summary>Gets the role that sees every document.</summary>
    public Role Auditor { get; }

    /// <summary>Gets the role that sees only its own documents.</summary>
    public Role Reader { get; }

    /// <summary>Gets a role without any document permission.</summary>
    public Role Outsider { get; }

    /// <summary>Gets the role of the file signing tool.</summary>
    public Role Tool { get; }

    /// <summary>Gets an author in the HR division of A.</summary>
    public User AuthorA { get; }

    /// <summary>Gets a signer in the HR division of A.</summary>
    public User SignerA { get; }

    /// <summary>Gets a signer at the top of A.</summary>
    public User SignerA2 { get; }

    /// <summary>Gets a signer in the subsidiary S.</summary>
    public User SignerS { get; }

    /// <summary>Gets a signer in organization B.</summary>
    public User SignerB { get; }

    /// <summary>Gets the document manager of A (sees the organization).</summary>
    public User RegistrarA { get; }

    /// <summary>Gets a viewer of the HR division's documents.</summary>
    public User UnitViewerHr { get; }

    /// <summary>Gets a viewer of every document (works in B).</summary>
    public User AuditorB { get; }

    /// <summary>Gets a user who sees only own documents.</summary>
    public User ReaderA { get; }

    /// <summary>Gets a user without document permissions.</summary>
    public User OutsiderA { get; }

    /// <summary>Gets a user of the file signing tool.</summary>
    public User ToolUser { get; }

    /// <summary>Gets the clock.</summary>
    public ManualClock Clock { get; } = new(Start);

    /// <summary>Gets the private file storage.</summary>
    public HashingFileStorage Files { get; } = new();

    /// <summary>Gets the scripted signature inspector.</summary>
    public ScriptedSignatureInspector Inspector { get; } = new();

    /// <summary>Gets the audit entries.</summary>
    public RecordedAudit Audit { get; } = new();

    /// <summary>Gets the registration numbers.</summary>
    public CountingNumbers Numbers { get; } = new();

    /// <summary>Gets the image processor.</summary>
    public ScriptedImageProcessor Images { get; } = new();

    /// <summary>Gets the notifications sent to the recipients' pages.</summary>
    public TwoOrganizations.RecordedNotificationEvents NotificationEvents => World.NotificationEvents;

    /// <inheritdoc />
    public void Dispose() => World.Dispose();

    /// <summary>
    /// Opens another context on the same in-memory store (optionally with interceptors), e.g. to change rows behind the
    /// back of the shared context.
    /// </summary>
    /// <param name="interceptors">The interceptors.</param>
    /// <returns>The context.</returns>
    public OrganizationDbContext OpenContext(params IInterceptor[] interceptors)
    {
        var options = (DbContextOptions<OrganizationDbContext>)Db.GetService<IDbContextOptions>();
        var builder = new DbContextOptionsBuilder<OrganizationDbContext>(options);
        if (interceptors.Length > 0)
        {
            builder.AddInterceptors(interceptors);
        }

        return new OrganizationDbContext(builder.Options);
    }

    /// <summary>
    /// Builds the services as seen by a user with the effective permissions of their roles.
    /// </summary>
    /// <param name="user">The current user.</param>
    /// <param name="db">The database context the services use (the shared one by default).</param>
    /// <param name="permissions">Permissions that replace the effective ones.</param>
    /// <returns>The services.</returns>
    public Session As(User? user, OrganizationDbContext? db = null, IReadOnlySet<string>? permissions = null)
    {
        // The services of a user on the shared context are built once (building them and resolving the permissions
        // costs a database round trip and the reflection of the pipeline).
        if (db is null && permissions is null && user is not null && _sessions.TryGetValue(user.Id, out var cached))
        {
            return cached;
        }

        var session = Build(user, db, permissions);
        if (db is null && permissions is null && user is not null)
        {
            _sessions[user.Id] = session;
        }

        return session;
    }

    /// <summary>
    /// Adds a user with one main position.
    /// </summary>
    /// <param name="email">The e-mail.</param>
    /// <param name="lastName">The last name.</param>
    /// <param name="firstName">The first name.</param>
    /// <param name="unit">The unit.</param>
    /// <param name="role">The role.</param>
    /// <param name="save">Whether to save at once (the constructor saves everyone together).</param>
    /// <returns>The user.</returns>
    public User Person(string email, string lastName, string firstName, OrganizationUnit unit, Role role, bool save = true)
    {
        var user = new User(email, lastName, firstName, null) { NormalizedEmail = email.ToUpperInvariant() };
        World.Master.Users.Add(user);
        var member = new Member(MemberProfile.Of(user));
        member.SetPositions([(unit.Id, role.Id, (string?)null, true)]);
        Db.Members.Add(member);
        if (save)
        {
            Db.SaveChanges();
            World.Master.SaveChanges();
        }

        return user;
    }

    /// <summary>
    /// Builds the services as seen by a user.
    /// </summary>
    /// <param name="user">The current user.</param>
    /// <param name="db">The database context the services use (the shared one by default).</param>
    /// <param name="permissions">Permissions that replace the effective ones.</param>
    /// <returns>The services.</returns>
    private Session Build(User? user, OrganizationDbContext? db, IReadOnlySet<string>? permissions)
    {
        db ??= Db;
        permissions ??= user is null
            ? new HashSet<string>()
            : Task.Run(() => new PermissionResolver(Db, new Catalog(World.Master)).GetEffectivePermissionsAsync(user.Id, CancellationToken.None)).GetAwaiter().GetResult();
        var services = new ServiceCollection()
            .AddLogging()
            .AddApplication();

        services.AddSingleton(World.Master);
        services.AddSingleton(db);
        services.AddSingleton<IDomainEventSource>(World.Master);
        services.AddSingleton<IDomainEventSource>(db);
        services.AddSingleton<ICurrentUser>(new StaticUser(user?.Id, permissions));
        services.AddSingleton<TimeProvider>(Clock);
        services.AddSingleton<IImageProcessor>(Images);
        services.AddSingleton<IMediaStorage>(World.Storage);
        services.AddSingleton<IFileStorage>(Files);
        services.AddSingleton<IChatEvents>(World.ChatEvents);
        services.AddSingleton<INotificationEvents>(World.NotificationEvents);
        services.AddSingleton<INewsDiscussionEvents, SilentDiscussionEvents>();
        services.AddSingleton<IEmailOutbox>(World.Emails);
        services.AddSingleton<IAuditLogger>(Audit);
        services.AddSingleton<IMarkdownRenderer, PlainMarkdown>();
        services.AddSingleton<IRichTextSanitizer, RichTextSanitizer>();
        services.AddSingleton<ISignatureInspector>(Inspector);
        services.AddSingleton<IDocumentNumberGenerator>(Numbers);
        services.AddSingleton(Options.Create(new EmailOptions { SiteUrl = "https://odc.test/" }));
        return new Session(services.BuildServiceProvider());
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
        World.Master.Roles.Add(role);
        return role;
    }

    /// <summary>
    /// The services of one user (handlers wrapped in the production pipeline).
    /// </summary>
    /// <param name="provider">The container.</param>
    internal sealed class Session(IServiceProvider provider)
    {
        /// <summary>
        /// Resolves a service.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <returns>The service.</returns>
        public T Get<T>()
            where T : notnull
            => provider.GetRequiredService<T>();

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
            try
            {
                return (Task<TResponse>)contract.GetMethod(nameof(IRequestHandler<,>.HandleAsync))!.Invoke(handler, [request, TestContext.Current.CancellationToken])!;
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                throw exception.InnerException;
            }
        }
    }

    /// <summary>
    /// A clock the tests move by hand.
    /// </summary>
    /// <param name="now">The starting moment.</param>
    internal sealed class ManualClock(DateTimeOffset now) : TimeProvider
    {
        /// <summary>Gets or sets the current moment.</summary>
        public DateTimeOffset Now { get; set; } = now;

        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => Now;
    }

    /// <summary>
    /// Keeps files in memory with their real SHA-256 digests and records deletions.
    /// </summary>
    internal sealed class HashingFileStorage : IFileStorage
    {
        /// <summary>Gets the stored files by key.</summary>
        public Dictionary<string, byte[]> Stored { get; } = [];

        /// <summary>Gets the keys deleted so far.</summary>
        public List<string> Deleted { get; } = [];

        /// <inheritdoc />
        public async Task<StoredBlob> SaveAsync(Stream content, string category, long maxSize, CancellationToken cancellationToken)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            var key = $"{category}/{Guid.NewGuid():N}";
            Stored[key] = buffer.ToArray();
            return new StoredBlob(key, buffer.Length, Convert.ToHexStringLower(SHA256.HashData(buffer.ToArray())));
        }

        /// <inheritdoc />
        public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
            => Task.FromResult<Stream>(new MemoryStream(Stored[storageKey]));

        /// <inheritdoc />
        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
        {
            Stored.Remove(storageKey);
            Deleted.Add(storageKey);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns the keys stored under a category.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <returns>The keys.</returns>
        public IReadOnlyList<string> In(string category) => [.. Stored.Keys.Where(k => k.StartsWith(category + "/", StringComparison.Ordinal))];
    }

    /// <summary>
    /// Reads signature files written as <c>SIG:serial1,serial2,…</c>: one signature per serial number, the last one being
    /// the newest. Any other content cannot be parsed.
    /// </summary>
    internal sealed class ScriptedSignatureInspector : ISignatureInspector
    {
        /// <summary>
        /// Builds the content of a signature file.
        /// </summary>
        /// <param name="serials">The signers' certificate serial numbers.</param>
        /// <returns>The bytes.</returns>
        public static byte[] File(params string[] serials) => Encoding.UTF8.GetBytes("SIG:" + string.Join(',', serials));

        /// <summary>
        /// Returns the certificate the inspector reads for a serial number.
        /// </summary>
        /// <param name="serial">The serial number.</param>
        /// <returns>The certificate.</returns>
        public static SignerCertificate Certificate(string serial) => new($"Підписант {serial}", "ТОВ «Тест»", "1234567890", "КНЕДП «Тест»", serial, null, null);

        /// <inheritdoc />
        public SignerCertificate? TryReadSigner(ReadOnlyMemory<byte> signature, SignatureFormat format)
            => TryReadSigners(signature, format) is [.., var last] ? last : null;

        /// <inheritdoc />
        public IReadOnlyList<SignerCertificate>? TryReadSigners(ReadOnlyMemory<byte> signature, SignatureFormat format)
            => Serials(signature)?.Select(Certificate).ToList();

        /// <inheritdoc />
        public int? TryCountSignatures(ReadOnlyMemory<byte> signature, SignatureFormat format) => Serials(signature)?.Length;

        /// <summary>
        /// Parses the serial numbers.
        /// </summary>
        /// <param name="signature">The bytes.</param>
        /// <returns>The serial numbers, or <see langword="null"/>.</returns>
        private static string[]? Serials(ReadOnlyMemory<byte> signature)
        {
            var text = Encoding.UTF8.GetString(signature.Span);
            return text.StartsWith("SIG:", StringComparison.Ordinal) ? text[4..].Split(',', StringSplitOptions.RemoveEmptyEntries) : null;
        }
    }

    /// <summary>
    /// Records the audit entries.
    /// </summary>
    internal sealed class RecordedAudit : IAuditLogger
    {
        /// <summary>Gets the entries added to the unit of work.</summary>
        public List<(string Action, string? EntityType, string? EntityId, string? Details)> Entries { get; } = [];

        /// <summary>
        /// Returns the actions recorded so far.
        /// </summary>
        /// <returns>The actions.</returns>
        public IReadOnlyList<string> Actions() => [.. Entries.Select(static e => e.Action)];

        /// <inheritdoc />
        public void Add(string action, string? entityType = null, string? entityId = null, string? details = null)
            => Entries.Add((action, entityType, entityId, details));

        /// <inheritdoc />
        public Task WriteAsync(string action, Guid? userId, string? userName, string? details, CancellationToken cancellationToken)
        {
            Entries.Add((action, null, userId?.ToString(), details));
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Issues registration numbers <c>2026/000001</c>, <c>2026/000002</c> and so on.
    /// </summary>
    internal sealed class CountingNumbers : IDocumentNumberGenerator
    {
        /// <summary>
        /// The last issued number.
        /// </summary>
        private int _last;

        /// <summary>Gets or sets a value indicating whether the next call fails with a domain error (a broken number).</summary>
        public bool IssueInvalid { get; set; }

        /// <inheritdoc />
        public Task<string> NextAsync(CancellationToken cancellationToken)
            => Task.FromResult(IssueInvalid ? new string('9', 40) : $"2026/{++_last:D6}");
    }

    /// <summary>
    /// Accepts any file whose first byte is not zero as a 100×80 image and records the crops it was given.
    /// </summary>
    internal sealed class ScriptedImageProcessor : IImageProcessor
    {
        /// <summary>Gets the crops passed to the processor.</summary>
        public List<ImageCrop?> Crops { get; } = [];

        /// <inheritdoc />
        public Task<ProcessedImage?> ProcessAsync(ReadOnlyMemory<byte> source, MediaPurpose purpose, ImageCrop? crop, CancellationToken cancellationToken)
        {
            Crops.Add(crop);
            return Task.FromResult(source.Span[0] == 0 ? null : new ProcessedImage([1, 2, 3, 4], [5, 6], 100, 80));
        }
    }

    /// <summary>
    /// A fixed current user.
    /// </summary>
    /// <param name="id">The user identifier, or <see langword="null"/> for an anonymous caller.</param>
    /// <param name="permissions">The effective permissions.</param>
    private sealed class StaticUser(Guid? id, IReadOnlySet<string> permissions) : ICurrentUser
    {
        /// <inheritdoc />
        public bool IsAuthenticated => id is not null;

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
    /// A Markdown renderer that returns the source.
    /// </summary>
    private sealed class PlainMarkdown : IMarkdownRenderer
    {
        /// <inheritdoc />
        public string ToHtml(string markdown) => markdown;
    }
}
