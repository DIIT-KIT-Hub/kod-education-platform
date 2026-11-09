using Microsoft.Extensions.DependencyInjection;
using ODC.Web.Features.Admin.Users;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Email;
using ODC.Web.Shared.Kernel;

namespace ODC.UnitTests.Application.Admin;

/// <summary>
/// The security actions on another account (authenticator reset, ending all sign-ins, invitations) and the
/// spreadsheet export and import template: each within the administrator's organizations only, a shared account only
/// by someone whose scope covers it whole.
/// </summary>
public sealed class UserSecurityTests : IDisposable
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
    /// Initializes a new instance of the <see cref="UserSecurityTests"/> class.
    /// </summary>
    public UserSecurityTests()
    {
        _identity = new ScriptedIdentity(_world.Db);
    }

    public void Dispose() => _world.Dispose();

    [Fact]
    public async Task The_HR_specialist_resets_the_authenticator_of_an_employee_and_it_is_audited()
    {
        var result = await As(_world.HrA).SendAsync(new ResetUserAuthenticator.Command(_world.EmployeeA.Id));

        Assert.True(result.IsSuccess);
        Assert.Contains($"ResetAuthenticator {_world.EmployeeA.Email}", _identity.Calls);
        var entry = Assert.Single(_audit.Entries);
        Assert.Equal(AuditActions.AuthenticatorReset, entry.Action);
        Assert.Contains(_world.EmployeeA.Email!, entry.Details, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_failed_authenticator_reset_is_returned_and_not_audited()
    {
        _identity.Outcome = Error.Validation("Не вдалося.");

        var result = await As(_world.HrA).SendAsync(new ResetUserAuthenticator.Command(_world.EmployeeA.Id));

        Assert.Equal(ErrorType.Validation, result.Failure());
        Assert.Empty(_audit.Entries);
    }

    [Fact]
    public async Task Ending_all_sign_ins_also_unlocks_the_account_and_is_audited()
    {
        var result = await As(_world.HrA).SendAsync(new RevokeUserSessions.Command(_world.EmployeeA.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal([$"InvalidateSessions {_world.EmployeeA.Email}", $"Unlock {_world.EmployeeA.Email}"], _identity.Calls);
        Assert.Equal([AuditActions.SessionRevoked], _audit.Actions());
    }

    [Fact]
    public async Task Security_actions_on_an_account_of_another_organization_find_nothing()
    {
        var hr = As(_world.HrA);

        Assert.Equal(ErrorType.NotFound, (await hr.SendAsync(new ResetUserAuthenticator.Command(_world.EmployeeB.Id))).Failure());
        Assert.Equal(ErrorType.NotFound, (await hr.SendAsync(new RevokeUserSessions.Command(_world.EmployeeB.Id))).Failure());
        Assert.Equal(ErrorType.NotFound, (await hr.SendAsync(new SendUserInvitation.Command(_world.EmployeeB.Id))).Failure());
        Assert.Equal(ErrorType.NotFound, (await hr.SendAsync(new RevokeUserSessions.Command(Guid.NewGuid()))).Failure());
        Assert.Empty(_identity.Calls);
        Assert.Empty(_audit.Entries);
    }

    [Fact]
    public async Task Security_actions_on_an_account_shared_with_another_organization_are_forbidden()
    {
        var hr = As(_world.HrA);

        Assert.Equal(ErrorType.Forbidden, (await hr.SendAsync(new ResetUserAuthenticator.Command(_world.Shared.Id))).Failure());
        Assert.Equal(ErrorType.Forbidden, (await hr.SendAsync(new RevokeUserSessions.Command(_world.Shared.Id))).Failure());
        Assert.Equal(ErrorType.Forbidden, (await hr.SendAsync(new SendUserInvitation.Command(_world.Shared.Id))).Failure());
        Assert.Empty(_identity.Calls);
    }

    [Fact]
    public async Task People_without_the_security_permission_cannot_act_on_accounts()
    {
        var employee = As(_world.EmployeeA);

        Assert.Equal(ErrorType.Forbidden, (await employee.SendAsync(new ResetUserAuthenticator.Command(_world.EmployeeS.Id))).Failure());
        Assert.Equal(ErrorType.Forbidden, (await employee.SendAsync(new RevokeUserSessions.Command(_world.EmployeeS.Id))).Failure());
        Assert.Equal(ErrorType.Forbidden, (await employee.SendAsync(new SendUserInvitation.Command(_world.EmployeeS.Id))).Failure());
        Assert.Empty(_identity.Calls);
    }

    [Fact]
    public async Task An_invitation_goes_to_an_active_account_that_awaits_activation()
    {
        var result = await As(_world.HrA).SendAsync(new SendUserInvitation.Command(_world.EmployeeA.Id));

        Assert.True(result.IsSuccess);
        var mail = Assert.Single(_world.Emails.Sent);
        Assert.Equal(_world.EmployeeA.Email, mail.To);
        Assert.Contains("https://odc.test/account/activate", mail.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task No_invitation_for_an_account_that_already_has_a_password_or_is_blocked()
    {
        _world.EmployeeA.PasswordHash = "hash";
        _world.EmployeeS.SetActive(false);
        _world.Db.SaveChanges();
        var hr = As(_world.HrA);

        Assert.Equal(ErrorType.Validation, (await hr.SendAsync(new SendUserInvitation.Command(_world.EmployeeA.Id))).Failure());
        Assert.Equal(ErrorType.Validation, (await hr.SendAsync(new SendUserInvitation.Command(_world.EmployeeS.Id))).Failure());
        Assert.Empty(_world.Emails.Sent);
    }

    [Fact]
    public async Task Without_e_mail_settings_an_invitation_answers_that_e_mail_is_unavailable()
    {
        var result = await _world.As(_world.HrA, static services => services.AddSingleton<IEmailOutbox>(new DisabledOutbox())).SendAsync(new SendUserInvitation.Command(_world.EmployeeA.Id));

        Assert.Equal("email_unavailable", result.Error?.Code);
        Assert.Equal(ErrorType.Unavailable, result.Error?.Type);
    }

    [Fact]
    public async Task The_export_lists_only_the_people_and_positions_of_the_administrators_organizations()
    {
        var file = (await As(_world.HrA).SendAsync(new ExportUsers.Query(new UserQuery(), SpreadsheetFormat.Csv))).Ok();

        var (rows, format) = _world.Written!.Value;
        Assert.Equal(SpreadsheetFormat.Csv, format);
        Assert.DoesNotContain(rows, r => r.Email == _world.EmployeeB.Email || r.Email == _world.HelpDeskB.Email);
        Assert.DoesNotContain(rows, static r => r.UnitCode == "B-PROJ");
        Assert.Single(rows, r => r.Email == _world.Shared.Email);
        Assert.Contains(rows, r => r.Email == _world.EmployeeA.Email && r.UnitCode == "A-HR" && r.Status == "активний");
        Assert.Matches(@"^users-\d{8}-\d{4}\.csv$", file.FileName);
        Assert.Equal("text/csv", file.ContentType);
        Assert.Equal([AuditActions.UsersExported], _audit.Actions());
    }

    [Fact]
    public async Task The_export_follows_the_filter_and_marks_blocked_accounts()
    {
        _world.EmployeeA.SetActive(false);
        _world.Db.SaveChanges();

        await As(_world.HrA).SendAsync(new ExportUsers.Query(new UserQuery { Search = "employee.a" }, SpreadsheetFormat.Xlsx));

        var row = Assert.Single(_world.Written!.Value.Rows);
        Assert.Equal(_world.EmployeeA.Email, row.Email);
        Assert.Equal("заблокований", row.Status);
    }

    [Fact]
    public async Task A_cabinet_wide_export_includes_everyone()
    {
        await As(_world.Admin).SendAsync(new ExportUsers.Query(new UserQuery(), SpreadsheetFormat.Xlsx));

        Assert.Contains(_world.Written!.Value.Rows, r => r.Email == _world.EmployeeB.Email);
    }

    [Fact]
    public async Task Only_holders_of_the_export_permission_export()
    {
        Assert.Equal(ErrorType.Forbidden, (await As(_world.EmployeeA).SendAsync(new ExportUsers.Query(new UserQuery(), SpreadsheetFormat.Csv))).Failure());
        Assert.Null(_world.Written);
    }

    [Theory]
    [InlineData(SpreadsheetFormat.Xlsx, "users-import-template.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData(SpreadsheetFormat.Csv, "users-import-template.csv", "text/csv")]
    public async Task The_import_template_shows_sample_rows_in_the_chosen_format(SpreadsheetFormat format, string name, string type)
    {
        var file = (await As(_world.HrA).SendAsync(new GetUserImportTemplate.Query(format))).Ok();

        Assert.Equal(name, file.FileName);
        Assert.Equal(type, file.ContentType);
        Assert.Equal(3, _world.Written!.Value.Rows.Count);
        Assert.All(_world.Written.Value.Rows, static r => Assert.EndsWith("@example.com", r.Email, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Only_holders_of_the_import_permission_get_the_template()
    {
        Assert.Equal(ErrorType.Forbidden, (await As(_world.EmployeeA).SendAsync(new GetUserImportTemplate.Query(SpreadsheetFormat.Csv))).Failure());
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

    /// <summary>
    /// An outbox without e-mail settings.
    /// </summary>
    private sealed class DisabledOutbox : IEmailOutbox
    {
        /// <inheritdoc />
        public bool IsEnabled => false;

        /// <inheritdoc />
        public void Enqueue(EmailMessage message) => throw new InvalidOperationException("E-mail is off.");
    }
}
