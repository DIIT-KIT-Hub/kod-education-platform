using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using ODC.Application.Abstractions;
using ODC.Application.Common.Email;
using ODC.Domain.Access;
using ODC.Domain.Users;
using ODC.Infrastructure.Identity.Activation;
using ODC.Infrastructure.Persistence;
using ODC.UnitTests.Application;

namespace ODC.UnitTests.Infrastructure;

/// <summary>
/// Account activation: one-time codes by e-mail, limits, no account enumeration.
/// </summary>
public sealed partial class AccountActivationTests : IAsyncDisposable
{
    /// <summary>
    /// A password that satisfies the default Identity policy.
    /// </summary>
    private const string Password = "Correct-Horse-Battery-9";

    /// <summary>
    /// The clock of the test.
    /// </summary>
    private readonly ManualClock _clock = new(new DateTimeOffset(2026, 11, 7, 9, 0, 0, TimeSpan.Zero));

    /// <summary>
    /// The e-mails sent during the test.
    /// </summary>
    private readonly TwoOrganizations.RecordedEmails _emails = new();

    /// <summary>
    /// The service provider of the test.
    /// </summary>
    private readonly ServiceProvider _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountActivationTests"/> class.
    /// </summary>
    public AccountActivationTests()
    {
        var database = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options
            .UseInMemoryDatabase(database)
            .ConfigureWarnings(static w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        services.AddIdentityCore<User>(static options => options.User.RequireUniqueEmail = true)
            .AddRoles<Role>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
        services.AddSingleton<IEmailOutbox>(_emails);
        services.AddSingleton<IAuditLogger, SilentAudit>();
        services.AddSingleton<TimeProvider>(_clock);
        services.Configure<EmailOptions>(static options => options.SiteUrl = "https://odc.test");
        services.Configure<ActivationOptions>(static options => options.MinimumRequestDuration = TimeSpan.Zero);
        services.AddScoped<AccountActivationService>();
        _services = services.BuildServiceProvider();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _services.DisposeAsync();

    [Fact]
    public async Task Owner_proves_the_email_and_chooses_the_first_password()
    {
        var user = await CreateUserAsync("new.user@example.com");
        await using var scope = _services.CreateAsyncScope();
        var activation = scope.ServiceProvider.GetRequiredService<AccountActivationService>();

        var started = (await activation.StartAsync("  New.User@Example.com ", TestContext.Current.CancellationToken)).Ok();
        var flow = activation.ReadTicket(started.Ticket)!;
        Assert.Equal(ActivationStage.Code, started.Status.Stage);
        Assert.Equal("new.user@example.com", flow.Email);

        var email = Assert.Single(_emails.Sent);
        Assert.Equal("new.user@example.com", email.To);
        var code = CodeIn(email);
        Assert.DoesNotContain(code, email.Subject, StringComparison.Ordinal);

        var verified = (await activation.VerifyAsync(flow, $"{code[..3]} {code[3..]}", TestContext.Current.CancellationToken)).Ok();
        Assert.False(verified.HasAuthenticator);
        Assert.Equal(ActivationStage.Password, (await activation.GetStatusAsync(flow, TestContext.Current.CancellationToken)).Stage);

        var activated = (await activation.CompleteAsync(flow, Password, TestContext.Current.CancellationToken)).Ok();
        Assert.Equal(user.Id, activated.Id);
        Assert.False(activated.AwaitsActivation);
        Assert.True(activated.EmailConfirmed);

        // The activation is used up: the same browser cannot set a password again.
        Assert.Equal(AccountActivationService.ExpiredCode, (await activation.CompleteAsync(flow, "Another-Password-77", TestContext.Current.CancellationToken)).Error?.Code);
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().ActivationChallenges.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Addresses_without_an_account_waiting_for_activation_get_the_same_answer_but_no_email()
    {
        var active = await CreateUserAsync("active@example.com");
        await using var scope = _services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        await users.AddPasswordAsync((await users.FindByIdAsync(active.Id.ToString()))!, Password);
        var activation = scope.ServiceProvider.GetRequiredService<AccountActivationService>();

        var unknown = (await activation.StartAsync("nobody@example.com", TestContext.Current.CancellationToken)).Ok();
        var activated = (await activation.StartAsync("active@example.com", TestContext.Current.CancellationToken)).Ok();

        Assert.Empty(_emails.Sent);
        Assert.Equal(ActivationStage.Code, unknown.Status.Stage);
        Assert.Equal(unknown.Status.ResendAfterSeconds, activated.Status.ResendAfterSeconds);

        // No code can open the password step of an address without an eligible account.
        var flow = activation.ReadTicket(unknown.Ticket)!;
        Assert.Equal(AccountActivationService.InvalidCode, (await activation.VerifyAsync(flow, "123456", TestContext.Current.CancellationToken)).Error?.Code);
    }

    [Fact]
    public async Task Too_many_wrong_codes_end_the_code_even_for_the_right_one()
    {
        await CreateUserAsync("guess@example.com");
        await using var scope = _services.CreateAsyncScope();
        var activation = scope.ServiceProvider.GetRequiredService<AccountActivationService>();
        var flow = activation.ReadTicket((await activation.StartAsync("guess@example.com", TestContext.Current.CancellationToken)).Ok().Ticket)!;
        var code = CodeIn(Assert.Single(_emails.Sent));
        var wrong = code == "000000" ? "111111" : "000000";

        for (var i = 0; i < 4; i++)
        {
            Assert.Equal(AccountActivationService.InvalidCode, (await activation.VerifyAsync(flow, wrong, TestContext.Current.CancellationToken)).Error?.Code);
        }

        Assert.Equal(AccountActivationService.ExpiredCode, (await activation.VerifyAsync(flow, wrong, TestContext.Current.CancellationToken)).Error?.Code);
        Assert.Equal(AccountActivationService.ExpiredCode, (await activation.VerifyAsync(flow, code, TestContext.Current.CancellationToken)).Error?.Code);
    }

    [Fact]
    public async Task Codes_expire_and_only_the_newest_one_works()
    {
        await CreateUserAsync("resend@example.com");
        await using var scope = _services.CreateAsyncScope();
        var activation = scope.ServiceProvider.GetRequiredService<AccountActivationService>();

        var first = (await activation.StartAsync("resend@example.com", TestContext.Current.CancellationToken)).Ok();
        _clock.Advance(TimeSpan.FromSeconds(20));

        // During the pause the same request is returned and no second e-mail goes out.
        var again = (await activation.StartAsync("resend@example.com", TestContext.Current.CancellationToken)).Ok();
        Assert.Single(_emails.Sent);
        Assert.Equal(activation.ReadTicket(first.Ticket)!.ChallengeId, activation.ReadTicket(again.Ticket)!.ChallengeId);
        Assert.InRange(again.Status.ResendAfterSeconds, 39, 40);

        _clock.Advance(TimeSpan.FromSeconds(41));
        var second = (await activation.StartAsync("resend@example.com", TestContext.Current.CancellationToken)).Ok();
        Assert.Equal(2, _emails.Sent.Count);
        var oldCode = CodeIn(_emails.Sent[0]);
        Assert.Equal(AccountActivationService.ExpiredCode, (await activation.VerifyAsync(activation.ReadTicket(first.Ticket)!, oldCode, TestContext.Current.CancellationToken)).Error?.Code);

        _clock.Advance(TimeSpan.FromMinutes(11));
        Assert.Equal(AccountActivationService.ExpiredCode, (await activation.VerifyAsync(activation.ReadTicket(second.Ticket)!, CodeIn(_emails.Sent[1]), TestContext.Current.CancellationToken)).Error?.Code);
    }

    [Fact]
    public async Task An_address_can_request_only_a_few_codes_per_hour()
    {
        await using var scope = _services.CreateAsyncScope();
        var activation = scope.ServiceProvider.GetRequiredService<AccountActivationService>();
        for (var i = 0; i < 5; i++)
        {
            Assert.True((await activation.StartAsync("flood@example.com", TestContext.Current.CancellationToken)).IsSuccess);
            _clock.Advance(TimeSpan.FromMinutes(1));
        }

        Assert.Equal("too_many_codes", (await activation.StartAsync("flood@example.com", TestContext.Current.CancellationToken)).Error?.Code);
    }

    [Fact]
    public void Tampered_or_foreign_tickets_are_rejected()
    {
        using var scope = _services.CreateScope();
        var activation = scope.ServiceProvider.GetRequiredService<AccountActivationService>();

        Assert.Null(activation.ReadTicket(null));
        Assert.Null(activation.ReadTicket("not-a-ticket"));
        Assert.Null(activation.ReadTicket(Guid.NewGuid().ToString("N")));
    }

    /// <summary>
    /// Finds the six-digit code in an e-mail.
    /// </summary>
    /// <param name="email">The e-mail.</param>
    /// <returns>The code.</returns>
    private static string CodeIn(EmailMessage email) => SixDigits().Match(email.Text).Value;

    /// <summary>
    /// Matches six digits standing alone.
    /// </summary>
    /// <returns>The regular expression.</returns>
    [GeneratedRegex(@"(?<!\d)\d{6}(?!\d)")]
    private static partial Regex SixDigits();

    /// <summary>
    /// Creates an account waiting for activation.
    /// </summary>
    /// <param name="email">The e-mail.</param>
    /// <returns>The user.</returns>
    private async Task<User> CreateUserAsync(string email)
    {
        await using var scope = _services.CreateAsyncScope();
        var user = new User(email, "Тестенко", "Тест", null);
        var result = await scope.ServiceProvider.GetRequiredService<UserManager<User>>().CreateAsync(user);
        Assert.True(result.Succeeded);
        Assert.True(user.AwaitsActivation);
        return user;
    }

    /// <summary>
    /// A clock the test moves by hand.
    /// </summary>
    /// <param name="now">The starting moment.</param>
    private sealed class ManualClock(DateTimeOffset now) : TimeProvider
    {
        /// <summary>
        /// The current moment.
        /// </summary>
        private DateTimeOffset _now = now;

        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => _now;

        /// <summary>
        /// Moves the clock forward.
        /// </summary>
        /// <param name="by">The time to add.</param>
        public void Advance(TimeSpan by) => _now += by;
    }

    /// <summary>
    /// An audit logger that records nothing.
    /// </summary>
    private sealed class SilentAudit : IAuditLogger
    {
        /// <inheritdoc />
        public void Add(string action, string? entityType = null, string? entityId = null, string? details = null)
        {
        }

        /// <inheritdoc />
        public Task WriteAsync(string action, Guid? userId, string? userName, string? details, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
