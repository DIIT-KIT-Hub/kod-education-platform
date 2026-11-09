using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using ODC.Web.Infrastructure.Identity;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;

namespace ODC.UnitTests.Infrastructure;

/// <summary>
/// The identity service on a real <see cref="UserManager{TUser}"/> configured like the cabinet (password rules, lockout,
/// the replay-protected authenticator, encrypted secrets, Ukrainian errors, the breached password check): accounts
/// without passwords, password changes that count wrong guesses, authenticator codes, recovery codes and unlocking.
/// </summary>
public sealed class IdentityServiceTests : IAsyncDisposable
{
    /// <summary>
    /// A password that satisfies the rules.
    /// </summary>
    private const string Password = "Correct-Horse-Battery-9";

    /// <summary>
    /// The clock of the authenticator codes.
    /// </summary>
    private readonly ManualClock _clock = new(new DateTimeOffset(2026, 11, 9, 9, 0, 0, TimeSpan.Zero));

    /// <summary>
    /// The breached password service.
    /// </summary>
    private readonly PwnedPasswords _pwned = new();

    /// <summary>
    /// The services.
    /// </summary>
    private readonly ServiceProvider _services;

    /// <summary>
    /// The scope of the test.
    /// </summary>
    private readonly AsyncServiceScope _scope;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityServiceTests"/> class.
    /// </summary>
    public IdentityServiceTests()
    {
        var database = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<MasterDbContext>(options => options
            .UseInMemoryDatabase(database)
            .ConfigureWarnings(static w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        services.AddIdentityCore<User>(static options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 12;
                options.Password.RequiredUniqueChars = 5;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<Role>()
            .AddEntityFrameworkStores<MasterDbContext>()
            .AddUserStore<ProtectedUserStore>()
            .AddErrorDescriber<UkrainianIdentityErrorDescriber>()
            .AddPasswordValidator<MaximumLengthPasswordValidator>()
            .AddPasswordValidator<PwnedPasswordValidator>()
            .AddDefaultTokenProviders()
            .AddTokenProvider<ReplayProtectedAuthenticatorTokenProvider>(TokenOptions.DefaultAuthenticatorProvider);
        services.AddHttpClient(PwnedPasswordValidator.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => _pwned);
        services.AddHttpContextAccessor();
        services.Configure<PasswordSecurityOptions>(options =>
        {
            options.MaximumLength = 64;
            options.BreachedPasswordsApi = new Uri("https://pwned.test/range/");
        });
        services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
        services.AddSingleton<UserTokenProtector>();
        services.AddSingleton<TimeProvider>(_clock);
        services.AddScoped<IIdentityService, IdentityService>();
        _services = services.BuildServiceProvider();
        _scope = _services.CreateAsyncScope();
    }

    /// <summary>
    /// Gets the identity service.
    /// </summary>
    private IIdentityService Identity => _scope.ServiceProvider.GetRequiredService<IIdentityService>();

    /// <summary>
    /// Gets the user manager.
    /// </summary>
    private UserManager<User> Users => _scope.ServiceProvider.GetRequiredService<UserManager<User>>();

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _scope.DisposeAsync();
        await _services.DisposeAsync();
        _pwned.Dispose();
    }

    [Fact]
    public async Task An_account_is_created_without_a_password_and_waits_for_activation()
    {
        var user = new User("new@example.com", "Нова", "Людина", null);

        Assert.True((await Identity.CreateAsync(user)).IsSuccess);

        Assert.True(user.AwaitsActivation);
        Assert.NotNull(await Users.FindByEmailAsync("NEW@example.com"));
    }

    [Fact]
    public async Task A_second_account_with_the_same_e_mail_is_refused_in_Ukrainian()
    {
        await CreateAsync("same@example.com");

        var result = await Identity.CreateAsync(new User("same@example.com", "Інша", "Людина", null));

        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Equal("identity", result.Error?.Code);
        Assert.Contains("Адреса «same@example.com» вже використовується.", result.Error!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_password_changes_with_the_right_current_one_and_ends_other_sessions()
    {
        var user = await CreateAsync("p@example.com", Password);
        var stamp = user.SecurityStamp;

        var result = await Identity.ChangePasswordAsync(user, Password, "Another-Strong-Pass-77");

        Assert.True(result.IsSuccess);
        Assert.NotEqual(stamp, user.SecurityStamp);
        Assert.True(await Users.CheckPasswordAsync(user, "Another-Strong-Pass-77"));
    }

    [Fact]
    public async Task A_wrong_current_password_counts_toward_the_lockout()
    {
        var user = await CreateAsync("p@example.com", Password);

        var result = await Identity.ChangePasswordAsync(user, "Wrong-Password-123", "Another-Strong-Pass-77");

        Assert.Equal("Неправильний поточний пароль.", result.Error?.Message);
        Assert.Equal(1, await Users.GetAccessFailedCountAsync(user));
    }

    [Fact]
    public async Task After_five_wrong_guesses_even_the_right_password_cannot_change_it()
    {
        var user = await CreateAsync("p@example.com", Password);
        for (var i = 0; i < 5; i++)
        {
            await Identity.ChangePasswordAsync(user, $"Wrong-Password-{i}00", "Another-Strong-Pass-77");
        }

        var result = await Identity.ChangePasswordAsync(user, Password, "Another-Strong-Pass-77");

        Assert.Equal("Забагато невдалих спроб. Спробуйте пізніше.", result.Error?.Message);
        Assert.True(await Users.CheckPasswordAsync(user, Password));
    }

    [Theory]
    [InlineData("Short-1a", "Пароль має містити щонайменше 12 символів.")]
    [InlineData("no-digits-in-here-Abc", "Пароль має містити цифру.")]
    [InlineData("NO-LOWER-CASE-123", "Пароль має містити малу латинську літеру.")]
    [InlineData("no-upper-case-123", "Пароль має містити велику латинську літеру.")]
    [InlineData("Aa1Aa1Aa1Aa1Aa1", "Пароль має містити щонайменше 5 різних символів.")]
    public async Task A_new_password_must_follow_the_rules(string password, string message)
    {
        var user = await CreateAsync("p@example.com", Password);

        var result = await Identity.ChangePasswordAsync(user, Password, password);

        Assert.Contains(message, result.Error?.Message, StringComparison.Ordinal);
        Assert.True(await Users.CheckPasswordAsync(user, Password));
    }

    [Fact]
    public async Task A_password_longer_than_the_limit_is_refused()
    {
        var user = await CreateAsync("p@example.com", Password);

        var result = await Identity.ChangePasswordAsync(user, Password, "Aa1-" + new string('x', 61));

        Assert.Contains("Пароль не може бути довшим за 64 символів.", result.Error?.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_password_found_in_breaches_is_refused_and_only_the_hash_prefix_leaves_the_server()
    {
        var user = await CreateAsync("p@example.com", Password);
        const string breached = "Breached-Password-2024";
        var hash = RangeHash(breached);
        _pwned.Answer = $"0000000000000000000000000000000000A:0\r\n{hash[5..]}:31337\r\n";

        var result = await Identity.ChangePasswordAsync(user, Password, breached);

        Assert.Equal("Цей пароль трапляється у відомих витоках даних. Оберіть інший.", result.Error?.Message);
        var request = Assert.Single(_pwned.Requests);
        Assert.Equal($"https://pwned.test/range/{hash[..5]}", request.Url);
        Assert.True(request.Padded);
    }

    [Fact]
    public async Task Padding_lines_with_a_zero_count_are_not_breaches()
    {
        var user = await CreateAsync("p@example.com", Password);
        const string candidate = "Fresh-Password-2026";
        var hash = RangeHash(candidate);
        _pwned.Answer = $"{hash[5..]}:0\r\n";

        Assert.True((await Identity.ChangePasswordAsync(user, Password, candidate)).IsSuccess);
    }

    [Fact]
    public async Task When_the_breach_service_is_down_the_password_is_accepted()
    {
        var user = await CreateAsync("p@example.com", Password);
        _pwned.Status = HttpStatusCode.ServiceUnavailable;

        Assert.True((await Identity.ChangePasswordAsync(user, Password, "Fresh-Password-2026")).IsSuccess);
    }

    [Fact]
    public async Task With_the_breach_check_off_no_request_is_made()
    {
        var user = await CreateAsync("p@example.com", Password);
        _scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<PasswordSecurityOptions>>().Value.CheckBreachedPasswords = false;

        Assert.True((await Identity.ChangePasswordAsync(user, Password, "Fresh-Password-2026")).IsSuccess);
        Assert.Empty(_pwned.Requests);
    }

    [Fact]
    public async Task A_password_reset_removes_the_password_and_ends_the_sessions()
    {
        var user = await CreateAsync("p@example.com", Password);
        var stamp = user.SecurityStamp;

        Assert.True((await Identity.RemovePasswordAsync(user)).IsSuccess);

        Assert.True(user.AwaitsActivation);
        Assert.NotEqual(stamp, user.SecurityStamp);
    }

    [Fact]
    public async Task Resetting_an_account_that_already_waits_only_ends_the_sessions()
    {
        var user = await CreateAsync("p@example.com");
        var stamp = user.SecurityStamp;

        Assert.True((await Identity.RemovePasswordAsync(user)).IsSuccess);

        Assert.True(user.AwaitsActivation);
        Assert.NotEqual(stamp, user.SecurityStamp);
    }

    [Fact]
    public async Task Ending_all_sessions_changes_the_security_stamp()
    {
        var user = await CreateAsync("p@example.com", Password);
        var stamp = user.SecurityStamp;

        await Identity.InvalidateSessionsAsync(user);

        Assert.NotEqual(stamp, user.SecurityStamp);
    }

    [Fact]
    public async Task The_current_authenticator_code_is_accepted_once_with_spaces_or_dashes()
    {
        var (user, secret) = await EnrolledAsync();
        var code = Code(secret, 0);

        Assert.True(await Identity.VerifyAuthenticatorCodeAsync(user, $"{code[..3]} {code[3..]}"));
        Assert.False(await Identity.VerifyAuthenticatorCodeAsync(user, $"{code[..3]}-{code[3..]}"));
    }

    [Fact]
    public async Task The_code_of_the_neighbouring_step_is_accepted_but_not_older_ones()
    {
        var (user, secret) = await EnrolledAsync();

        Assert.False(await Identity.VerifyAuthenticatorCodeAsync(user, Code(secret, -2)));
        Assert.True(await Identity.VerifyAuthenticatorCodeAsync(user, Code(secret, -1)));
    }

    [Fact]
    public async Task After_a_code_is_used_older_codes_of_the_window_are_refused_too()
    {
        var (user, secret) = await EnrolledAsync();

        Assert.True(await Identity.VerifyAuthenticatorCodeAsync(user, Code(secret, 1)));
        Assert.False(await Identity.VerifyAuthenticatorCodeAsync(user, Code(secret, 0)));

        _clock.Advance(TimeSpan.FromSeconds(60));
        Assert.True(await Identity.VerifyAuthenticatorCodeAsync(user, Code(secret, 0)));
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("12a456")]
    [InlineData("+12345")]
    [InlineData("")]
    public async Task Malformed_codes_are_refused_and_counted(string code)
    {
        var (user, _) = await EnrolledAsync();

        Assert.False(await Identity.VerifyAuthenticatorCodeAsync(user, code));
        Assert.Equal(1, await Users.GetAccessFailedCountAsync(user));
    }

    [Fact]
    public async Task A_right_code_resets_the_failure_count()
    {
        var (user, secret) = await EnrolledAsync();
        await Identity.VerifyAuthenticatorCodeAsync(user, "000000");

        Assert.True(await Identity.VerifyAuthenticatorCodeAsync(user, Code(secret, 0)));
        Assert.Equal(0, await Users.GetAccessFailedCountAsync(user));
    }

    [Fact]
    public async Task A_locked_out_account_cannot_confirm_with_a_right_code_until_unlocked()
    {
        var (user, secret) = await EnrolledAsync();
        for (var i = 0; i < 5; i++)
        {
            await Identity.VerifyAuthenticatorCodeAsync(user, "000000");
        }

        Assert.True(await Users.IsLockedOutAsync(user));
        Assert.False(await Identity.VerifyAuthenticatorCodeAsync(user, Code(secret, 0)));

        await Identity.UnlockAsync(user);
        Assert.False(await Users.IsLockedOutAsync(user));
        Assert.Equal(0, await Users.GetAccessFailedCountAsync(user));
        Assert.True(await Identity.VerifyAuthenticatorCodeAsync(user, Code(secret, 0)));
    }

    [Fact]
    public async Task Without_an_authenticator_no_code_is_valid()
    {
        var user = await CreateAsync("p@example.com", Password);

        Assert.False(await Identity.VerifyAuthenticatorCodeAsync(user, "123456"));
    }

    [Fact]
    public async Task Ten_recovery_codes_are_issued_each_works_once_and_they_are_stored_encrypted()
    {
        var (user, _) = await EnrolledAsync();

        var codes = await Identity.GenerateRecoveryCodesAsync(user);

        Assert.Equal(10, codes.Count);
        Assert.Equal(10, codes.Distinct().Count());
        Assert.Equal(10, await Identity.CountRecoveryCodesAsync(user));
        Assert.True((await Users.RedeemTwoFactorRecoveryCodeAsync(user, codes[0])).Succeeded);
        Assert.False((await Users.RedeemTwoFactorRecoveryCodeAsync(user, codes[0])).Succeeded);
        Assert.Equal(9, await Identity.CountRecoveryCodesAsync(user));

        var stored = await _scope.ServiceProvider.GetRequiredService<MasterDbContext>().UserTokens
            .Where(t => t.UserId == user.Id)
            .Select(static t => t.Value)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.All(stored, value => Assert.DoesNotContain(codes[1], value!, StringComparison.Ordinal));
        Assert.All(stored, static value => Assert.True(UserTokenProtector.IsProtected(value) || long.TryParse(value, out _)));
    }

    [Fact]
    public async Task Resetting_the_authenticator_turns_off_two_factor_drops_the_recovery_codes_and_changes_the_key()
    {
        var (user, secret) = await EnrolledAsync();
        await Identity.GenerateRecoveryCodesAsync(user);
        var key = await Users.GetAuthenticatorKeyAsync(user);

        Assert.True((await Identity.ResetAuthenticatorAsync(user)).IsSuccess);

        Assert.False(user.TwoFactorEnabled);
        Assert.Equal(0, await Identity.CountRecoveryCodesAsync(user));
        Assert.NotEqual(key, await Users.GetAuthenticatorKeyAsync(user));
        Assert.False(await Identity.VerifyAuthenticatorCodeAsync(user, Code(secret, 0)));
    }

    [Fact]
    public async Task The_principal_carries_names_language_photo_and_the_resolved_permissions_but_not_the_stored_adjustments()
    {
        var user = await CreateAsync("claims@example.com", Password);
        var photo = Guid.NewGuid();
        user.SetLanguage("en");
        user.SetPhoto(photo);
        await Users.UpdateAsync(user);
        await Users.AddClaimAsync(user, new System.Security.Claims.Claim(PermissionClaimTypes.DeniedPermission, "news.view"));
        await Users.AddClaimAsync(user, new System.Security.Claims.Claim(PermissionClaimTypes.Permission, "audit.view"));
        var factory = new AppUserClaimsPrincipalFactory(
            Users,
            Microsoft.Extensions.Options.Options.Create(new IdentityOptions()),
            new FixedPermissions("tickets.manage", "chats.use"),
            new Microsoft.AspNetCore.Http.HttpContextAccessor());

        var principal = await factory.CreateAsync(user);

        Assert.Equal(["chats.use", "tickets.manage"], principal.FindAll(AppClaimTypes.Permission).Select(static c => c.Value));
        Assert.Empty(principal.FindAll(PermissionClaimTypes.DeniedPermission));
        Assert.Equal("Тест Користувач", principal.FindFirst(AppClaimTypes.FullName)?.Value);
        Assert.Equal(user.ShortName, principal.FindFirst(AppClaimTypes.ShortName)?.Value);
        Assert.Equal("en", principal.FindFirst(AppClaimTypes.Language)?.Value);
        Assert.Equal(photo.ToString("N"), principal.FindFirst(AppClaimTypes.Photo)?.Value);
    }

    [Fact]
    public async Task A_principal_without_a_language_or_photo_has_no_such_claims()
    {
        var user = await CreateAsync("plain@example.com", Password);
        var factory = new AppUserClaimsPrincipalFactory(
            Users,
            Microsoft.Extensions.Options.Options.Create(new IdentityOptions()),
            new FixedPermissions(),
            new Microsoft.AspNetCore.Http.HttpContextAccessor());

        var principal = await factory.CreateAsync(user);

        Assert.Null(principal.FindFirst(AppClaimTypes.Language));
        Assert.Null(principal.FindFirst(AppClaimTypes.Photo));
        Assert.Empty(principal.FindAll(AppClaimTypes.Permission));
    }

    /// <summary>
    /// The SHA-1 hash the breached password service is keyed by.
    /// </summary>
    /// <param name="password">The password.</param>
    /// <returns>The upper-case hex hash.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms", Justification = "The range API is keyed by SHA-1.")]
    private static string RangeHash(string password) => Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(password)));

    /// <summary>
    /// Creates an account, optionally with a password.
    /// </summary>
    /// <param name="email">The e-mail.</param>
    /// <param name="password">The password, or null for an account awaiting activation.</param>
    /// <returns>The user.</returns>
    private async Task<User> CreateAsync(string email, string? password = null)
    {
        var user = new User(email, "Тест", "Користувач", null);
        Assert.True((await Identity.CreateAsync(user)).IsSuccess);
        if (password is not null)
        {
            Assert.True((await Users.AddPasswordAsync(user, password)).Succeeded);
        }

        _pwned.Requests.Clear();
        return user;
    }

    /// <summary>
    /// Creates an account with a password and an enrolled authenticator.
    /// </summary>
    /// <returns>The user and the authenticator secret.</returns>
    private async Task<(User User, byte[] Secret)> EnrolledAsync()
    {
        var user = await CreateAsync("totp@example.com", Password);
        await Users.ResetAuthenticatorKeyAsync(user);
        await Users.SetTwoFactorEnabledAsync(user, true);
        return (user, Totp.DecodeBase32((await Users.GetAuthenticatorKeyAsync(user))!)!);
    }

    /// <summary>
    /// The authenticator code of a step relative to now.
    /// </summary>
    /// <param name="secret">The secret.</param>
    /// <param name="offset">The offset in steps.</param>
    /// <returns>The six-digit code.</returns>
    private string Code(byte[] secret, int offset)
        => Totp.Compute(secret, Totp.TimeStep(_clock.GetUtcNow()) + offset).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// A clock the test moves.
    /// </summary>
    /// <param name="now">The start time.</param>
    private sealed class ManualClock(DateTimeOffset now) : TimeProvider
    {
        /// <summary>
        /// The current time.
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
    /// A permission resolver with a fixed answer.
    /// </summary>
    /// <param name="permissions">The effective permissions.</param>
    private sealed class FixedPermissions(params string[] permissions) : ODC.Web.Shared.Access.IPermissionResolver
    {
        /// <inheritdoc />
        public Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlySet<string>>(permissions.ToHashSet());
    }

    /// <summary>
    /// The breached password range service.
    /// </summary>
    private sealed class PwnedPasswords : HttpMessageHandler
    {
        /// <summary>
        /// Gets the requests made (address and whether padding was asked for).
        /// </summary>
        public List<(string Url, bool Padded)> Requests { get; } = [];

        /// <summary>
        /// Gets or sets the answer body.
        /// </summary>
        public string Answer { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the answer status.
        /// </summary>
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.RequestUri!.ToString(), request.Headers.TryGetValues("Add-Padding", out var padding) && padding.Contains("true")));
            return Task.FromResult(new HttpResponseMessage(Status) { Content = new StringContent(Answer) });
        }
    }
}
