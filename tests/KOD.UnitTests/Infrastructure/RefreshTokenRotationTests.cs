using KOD.Infrastructure.Identity;
using KOD.Infrastructure.Identity.Tokens;
using KOD.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace KOD.UnitTests.Infrastructure;

/// <summary>
/// Refresh token rotation with replay detection (RFC 9700 §4.14.2) on an in-memory database.
/// </summary>
public sealed class RefreshTokenRotationTests : IDisposable
{
    /// <summary>
    /// The security stamp of the test user.
    /// </summary>
    private const string Stamp = "STAMP-1";

    /// <summary>
    /// The services (database context per scope).
    /// </summary>
    private readonly ServiceProvider _services;

    /// <summary>
    /// The controllable clock.
    /// </summary>
    private readonly ManualClock _clock = new(new DateTimeOffset(2026, 10, 30, 9, 0, 0, TimeSpan.Zero));

    /// <summary>
    /// The store under test.
    /// </summary>
    private readonly RefreshTokenStore _store;

    /// <summary>
    /// The test user.
    /// </summary>
    private readonly Guid _userId = Guid.CreateVersion7();

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenRotationTests"/> class.
    /// </summary>
    public RefreshTokenRotationTests()
    {
        var database = Guid.NewGuid().ToString();
        _services = new ServiceCollection()
            .AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(database))
            .BuildServiceProvider();
        _store = new RefreshTokenStore(
            _services.GetRequiredService<IServiceScopeFactory>(),
            new EphemeralDataProtectionProvider(),
            Options.Create(new SessionLifetimeOptions { IdleTimeout = TimeSpan.FromHours(1), AbsoluteLifetime = TimeSpan.FromHours(12) }),
            Options.Create(new JwtOptions { RefreshTokenReuseInterval = TimeSpan.FromSeconds(30) }),
            _clock,
            NullLogger<RefreshTokenStore>.Instance);
    }

    /// <inheritdoc />
    public void Dispose() => _services.Dispose();

    [Fact]
    public async Task Every_refresh_rotates_the_token()
    {
        var first = await SignInAsync();

        var second = await RefreshAsync(first.Token!);
        Assert.Equal(RefreshStatus.Refreshed, second.Status);
        Assert.NotEqual(first.Token, second.Token);
        Assert.Equal(first.SignIn!.Id, second.SignIn!.Id);

        _clock.Advance(TimeSpan.FromMinutes(5));
        var third = await RefreshAsync(second.Token!);
        Assert.Equal(RefreshStatus.Refreshed, third.Status);
        Assert.NotEqual(second.Token, third.Token);
        Assert.Equal(1, await FamiliesAsync());
    }

    [Fact]
    public async Task A_parallel_request_with_the_previous_token_gets_the_current_one_within_the_reuse_interval()
    {
        var first = await SignInAsync();
        var rotated = await RefreshAsync(first.Token!);

        _clock.Advance(TimeSpan.FromSeconds(10));
        var raced = await RefreshAsync(first.Token!);

        Assert.Equal(RefreshStatus.Refreshed, raced.Status);
        Assert.Equal(rotated.Token, raced.Token);
        Assert.Equal(RefreshStatus.Refreshed, (await RefreshAsync(rotated.Token!)).Status);
    }

    [Fact]
    public async Task A_rotated_token_coming_back_later_revokes_the_whole_family()
    {
        var first = await SignInAsync();
        var rotated = await RefreshAsync(first.Token!);

        // An attacker replays the stolen first token after the reuse interval.
        _clock.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(RefreshStatus.Reused, (await RefreshAsync(first.Token!)).Status);

        // The legitimate holder of the newest token is signed out too: the family is gone.
        Assert.Equal(RefreshStatus.Invalid, (await RefreshAsync(rotated.Token!)).Status);
        Assert.Equal(0, await FamiliesAsync());
    }

    [Fact]
    public async Task Any_older_generation_counts_as_reuse()
    {
        var first = await SignInAsync();
        var second = await RefreshAsync(first.Token!);
        _clock.Advance(TimeSpan.FromSeconds(5));
        var third = await RefreshAsync(second.Token!);
        Assert.Equal(RefreshStatus.Refreshed, third.Status);

        // Generation 0 is two rotations old: no grace, even inside the reuse interval of generation 1.
        Assert.Equal(RefreshStatus.Reused, (await RefreshAsync(first.Token!)).Status);
        Assert.Equal(0, await FamiliesAsync());
    }

    [Fact]
    public async Task Forged_tokens_are_rejected_without_touching_the_family()
    {
        var first = await SignInAsync();
        var bytes = System.Buffers.Text.Base64Url.DecodeFromChars(first.Token);
        bytes[^1] ^= 0x01;
        var forged = System.Buffers.Text.Base64Url.EncodeToString(bytes);

        Assert.Equal(RefreshStatus.Invalid, (await RefreshAsync(forged)).Status);
        Assert.Equal(RefreshStatus.Invalid, (await RefreshAsync("not-a-token")).Status);
        Assert.Equal(RefreshStatus.Refreshed, (await RefreshAsync(first.Token!)).Status);
    }

    [Fact]
    public async Task A_family_ends_after_the_idle_timeout_and_never_outlives_the_absolute_lifetime()
    {
        var idle = await SignInAsync();
        _clock.Advance(TimeSpan.FromHours(1).Add(TimeSpan.FromSeconds(1)));
        Assert.Equal(RefreshStatus.Invalid, (await RefreshAsync(idle.Token!)).Status);

        var active = await SignInAsync();
        var signedInAt = _clock.GetUtcNow();
        var token = active.Token!;
        RefreshResult last = active;
        for (var i = 0; i < 26; i++)
        {
            _clock.Advance(TimeSpan.FromMinutes(30));
            last = await RefreshAsync(token);
            if (last.Status != RefreshStatus.Refreshed)
            {
                break;
            }

            Assert.True(last.ExpiresAt <= signedInAt.AddHours(12));
            token = last.Token!;
        }

        Assert.Equal(RefreshStatus.Invalid, last.Status);
    }

    [Fact]
    public async Task Sign_out_revokes_the_family_and_a_rebind_follows_the_new_stamp()
    {
        var signedOut = await SignInAsync();
        await _store.RevokeAsync(signedOut.Token!, CancellationToken.None);
        Assert.Equal(RefreshStatus.Invalid, (await RefreshAsync(signedOut.Token!)).Status);

        var current = await SignInAsync();
        var id = current.SignIn!.Id;
        Assert.True(await _store.IsAliveAsync(id, _userId, Stamp, CancellationToken.None));

        // The password changed in this browser: the family follows the new stamp, the old stamp no longer matches.
        Assert.NotNull(await _store.RebindAsync(id, _userId, "STAMP-2", CancellationToken.None));
        Assert.True(await _store.IsAliveAsync(id, _userId, "STAMP-2", CancellationToken.None));
        Assert.False(await _store.IsAliveAsync(id, _userId, Stamp, CancellationToken.None));
        Assert.False(await _store.IsAliveAsync(id, Guid.CreateVersion7(), "STAMP-2", CancellationToken.None));
    }

    /// <summary>
    /// Signs the test user in.
    /// </summary>
    /// <returns>The first refresh token.</returns>
    private Task<RefreshResult> SignInAsync() => _store.CreateAsync(_userId, Stamp, "mfa", isPersistent: false, "127.0.0.1", "tests", CancellationToken.None);

    /// <summary>
    /// Presents a refresh token.
    /// </summary>
    /// <param name="token">The token.</param>
    /// <returns>The outcome.</returns>
    private Task<RefreshResult> RefreshAsync(string token) => _store.RefreshAsync(token, "127.0.0.1", CancellationToken.None);

    /// <summary>
    /// Counts the stored families.
    /// </summary>
    /// <returns>The count.</returns>
    private async Task<int> FamiliesAsync()
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().RefreshTokenFamilies.CountAsync();
    }

    /// <summary>
    /// A clock the test moves forward.
    /// </summary>
    /// <param name="start">The initial moment.</param>
    private sealed class ManualClock(DateTimeOffset start) : TimeProvider
    {
        /// <summary>
        /// The current moment.
        /// </summary>
        private DateTimeOffset _now = start;

        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => _now;

        /// <summary>
        /// Moves the clock forward.
        /// </summary>
        /// <param name="by">The amount.</param>
        public void Advance(TimeSpan by) => _now += by;
    }
}
