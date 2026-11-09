using Microsoft.EntityFrameworkCore;
using ODC.Web.Infrastructure.Auditing;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Identity;

namespace ODC.UnitTests.Infrastructure;

/// <summary>
/// The audit log writer: who acted, from where and when; <c>Add</c> joins the request's save, <c>WriteAsync</c> saves
/// at once (sign-in events that happen without a signed-in user).
/// </summary>
public sealed class AuditLoggerTests : IDisposable
{
    /// <summary>
    /// The time of the events.
    /// </summary>
    private static readonly DateTimeOffset Now = new(2026, 5, 1, 8, 30, 0, TimeSpan.Zero);

    /// <summary>
    /// The database.
    /// </summary>
    private readonly ApplicationDbContext _db = new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase($"audit-{Guid.NewGuid():N}").Options);

    /// <summary>
    /// Gets the cancellation token of the test.
    /// </summary>
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task An_added_entry_names_the_current_user_and_address_and_is_saved_with_the_request()
    {
        var userId = Guid.NewGuid();
        var logger = new AuditLogger(_db, new Actor(userId, "Петренко Іван", "203.0.113.7"), new Clock());

        logger.Add("document.created", "Document", "42", "Наказ № 1");

        Assert.Empty(await _db.AuditEntries.ToListAsync(Token));
        await _db.SaveChangesAsync(Token);
        var entry = Assert.Single(await _db.AuditEntries.ToListAsync(Token));
        Assert.Equal(Now, entry.OccurredAt);
        Assert.Equal("document.created", entry.Action);
        Assert.Equal(userId, entry.UserId);
        Assert.Equal("Петренко Іван", entry.UserName);
        Assert.Equal("Document", entry.EntityType);
        Assert.Equal("42", entry.EntityId);
        Assert.Equal("Наказ № 1", entry.Details);
        Assert.Equal("203.0.113.7", entry.IpAddress);
    }

    [Fact]
    public async Task An_entry_without_a_signed_in_user_keeps_only_the_address()
    {
        var logger = new AuditLogger(_db, new Actor(null, null, "198.51.100.1"), new Clock());

        logger.Add("auth.activation-failed");
        await _db.SaveChangesAsync(Token);

        var entry = Assert.Single(await _db.AuditEntries.ToListAsync(Token));
        Assert.Null(entry.UserId);
        Assert.Null(entry.UserName);
        Assert.Null(entry.EntityType);
        Assert.Equal("198.51.100.1", entry.IpAddress);
    }

    [Fact]
    public async Task A_written_entry_is_saved_at_once_for_the_named_user()
    {
        var userId = Guid.NewGuid();
        var logger = new AuditLogger(_db, new Actor(null, null, "192.0.2.10"), new Clock());

        await logger.WriteAsync("auth.sign-in", userId, "user@example.com", "Пароль і код", Token);

        var entry = Assert.Single(await _db.AuditEntries.ToListAsync(Token));
        Assert.Equal(userId, entry.UserId);
        Assert.Equal("user@example.com", entry.UserName);
        Assert.Equal(userId.ToString(), entry.EntityId);
        Assert.Equal("Пароль і код", entry.Details);
        Assert.Equal("192.0.2.10", entry.IpAddress);
    }

    [Fact]
    public async Task A_written_entry_for_an_unknown_account_has_no_user()
    {
        var logger = new AuditLogger(_db, new Actor(null, null, null), new Clock());

        await logger.WriteAsync("auth.sign-in-failed", null, "nobody@example.com", null, Token);

        var entry = Assert.Single(await _db.AuditEntries.ToListAsync(Token));
        Assert.Null(entry.UserId);
        Assert.Null(entry.EntityId);
        Assert.Equal("nobody@example.com", entry.UserName);
    }

    /// <summary>
    /// The user of the request.
    /// </summary>
    /// <param name="id">The user id.</param>
    /// <param name="name">The display name.</param>
    /// <param name="address">The IP address.</param>
    private sealed class Actor(Guid? id, string? name, string? address) : ICurrentUser
    {
        /// <inheritdoc />
        public bool IsAuthenticated => id is not null;

        /// <inheritdoc />
        public Guid? Id => id;

        /// <inheritdoc />
        public string? DisplayName => name;

        /// <inheritdoc />
        public string? IpAddress => address;

        /// <inheritdoc />
        public IReadOnlySet<string> Permissions { get; } = new HashSet<string>();

        /// <inheritdoc />
        public bool Has(string permission) => false;
    }

    /// <summary>
    /// A clock that stands at <see cref="Now"/>.
    /// </summary>
    private sealed class Clock : TimeProvider
    {
        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
