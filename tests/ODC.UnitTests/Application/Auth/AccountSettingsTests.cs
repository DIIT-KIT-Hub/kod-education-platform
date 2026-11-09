using ODC.Web.Features.Auth;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Kernel;

namespace ODC.UnitTests.Application.Auth;

/// <summary>
/// The account settings a signed-in user changes: the password and the interface language. (The sign-in steps run
/// through the sign-in manager and the HTTP context and are covered by the integration tests.)
/// </summary>
public sealed class AccountSettingsTests : IDisposable
{
    /// <summary>
    /// The cabinet with organizations A (with subsidiary S) and B.
    /// </summary>
    private readonly TwoOrganizations _world = new();

    /// <summary>
    /// The identity service the tests script.
    /// </summary>
    private readonly ScriptedIdentity _identity;

    /// <summary>
    /// The recorded audit events.
    /// </summary>
    private readonly RecordingAudit _audit = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountSettingsTests"/> class.
    /// </summary>
    public AccountSettingsTests()
    {
        _identity = new ScriptedIdentity(_world.Db);
    }

    public void Dispose() => _world.Dispose();

    [Fact]
    public async Task The_password_changes_through_the_identity_rules_and_is_audited()
    {
        var result = await As(_world.EmployeeA).SendAsync(new ChangePassword.Command("Old-password-1", "New-password-2"));

        Assert.True(result.IsSuccess);
        Assert.Contains($"ChangePassword {_world.EmployeeA.Email}", _identity.Calls);
        Assert.Contains(AuditActions.PasswordChanged, _audit.Actions());
    }

    [Fact]
    public async Task The_new_password_must_differ_from_the_current_one()
    {
        var result = await As(_world.EmployeeA).SendAsync(new ChangePassword.Command("Same-password-1", "Same-password-1"));

        Assert.Equal(ErrorType.Validation, result.Failure());
        Assert.Empty(_identity.Calls);
    }

    [Fact]
    public async Task A_refused_password_is_reported_and_not_audited()
    {
        _identity.Outcome = Error.Validation("Неправильний поточний пароль.");

        var result = await As(_world.EmployeeA).SendAsync(new ChangePassword.Command("Wrong-password-1", "New-password-2"));

        Assert.Equal(ErrorType.Validation, result.Failure());
        Assert.DoesNotContain(AuditActions.PasswordChanged, _audit.Actions());
    }

    [Theory]
    [InlineData("en")]
    [InlineData("uk")]
    public async Task The_language_is_saved_in_the_profile(string language)
    {
        Assert.True((await As(_world.EmployeeA).SendAsync(new SetLanguage.Command(language))).IsSuccess);

        Assert.Equal(language, _world.Db.Users.Single(u => u.Id == _world.EmployeeA.Id).Language);
    }

    [Fact]
    public async Task An_unknown_language_is_refused()
    {
        var result = await As(_world.EmployeeA).SendAsync(new SetLanguage.Command("de"));

        Assert.Equal(ErrorType.Validation, result.Failure());
        Assert.Null(_world.Db.Users.Single(u => u.Id == _world.EmployeeA.Id).Language);
    }

    /// <summary>
    /// The services of a user with the scripted identity service and the recorded audit.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>The services.</returns>
    private TwoOrganizations.Services As(User user) => _world.As(user, services =>
    {
        _identity.Register(services);
        _audit.Register(services);
    });
}
