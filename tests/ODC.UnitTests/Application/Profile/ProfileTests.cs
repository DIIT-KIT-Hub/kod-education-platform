using ODC.Web.Features.Profile;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Files;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Media;

namespace ODC.UnitTests.Application.Profile;

/// <summary>
/// The user's own profile: what it shows, what the user may change (names and e-mail only with the right to manage
/// their own account), the photo, new recovery codes and signing out elsewhere.
/// </summary>
public sealed class ProfileTests : IDisposable
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
    /// Initializes a new instance of the <see cref="ProfileTests"/> class.
    /// </summary>
    public ProfileTests()
    {
        _identity = new ScriptedIdentity(_world.Master);
    }

    public void Dispose() => _world.Dispose();

    [Fact]
    public async Task The_profile_shows_the_positions_main_first_the_modules_and_the_permissions()
    {
        var profile = (await As(_world.HrA).SendAsync(new GetProfile.Query())).Ok();

        Assert.Equal(_world.HrA.Email, profile.Email);
        Assert.Equal(2, profile.Positions.Count);
        Assert.True(profile.Positions[0].IsPrimary);
        Assert.Equal(_world.AHr.Id, profile.Positions[0].UnitId);
        Assert.Contains(AppModule.Users, profile.Modules);
        Assert.Contains(profile.Permissions, p => p.Code == Permissions.Users.Manage);
        Assert.DoesNotContain(profile.Permissions, p => p.Code == Permissions.Roles.Manage);
        Assert.Equal(7, profile.RecoveryCodesLeft);
    }

    [Fact]
    public async Task Only_an_administrator_whose_scope_covers_all_positions_edits_their_own_names()
    {
        Assert.False((await As(_world.EmployeeA).SendAsync(new GetProfile.Query())).Ok().CanEditPersonalData);

        // The HR specialist of A also works in B, where users.manage does not apply.
        Assert.False((await As(_world.HrA).SendAsync(new GetProfile.Query())).Ok().CanEditPersonalData);
        Assert.True((await As(_world.Admin).SendAsync(new GetProfile.Query())).Ok().CanEditPersonalData);
    }

    [Fact]
    public async Task Everyone_changes_their_phone()
    {
        var result = await As(_world.EmployeeA).SendAsync(new UpdateProfile.Command(new ProfileUpdateRequest(null, null, null, null, "+380501112233")));

        Assert.True(result.IsSuccess);
        Assert.Equal("+380501112233", Reload(_world.EmployeeA).PhoneNumber);
        Assert.Empty(_identity.Calls);
    }

    [Fact]
    public async Task An_employee_cannot_change_their_names_or_e_mail()
    {
        var employee = As(_world.EmployeeA);

        Assert.Equal(ErrorType.Forbidden, (await employee.SendAsync(new UpdateProfile.Command(new ProfileUpdateRequest("Нове", null, null, null, null)))).Failure());
        Assert.Equal(ErrorType.Forbidden, (await employee.SendAsync(new UpdateProfile.Command(new ProfileUpdateRequest(null, null, null, "new@example.com", null)))).Failure());
    }

    [Fact]
    public async Task Sending_the_same_names_changes_nothing_but_the_phone()
    {
        var current = Reload(_world.EmployeeA);
        var result = await As(_world.EmployeeA).SendAsync(new UpdateProfile.Command(new ProfileUpdateRequest(current.LastName, current.FirstName, current.MiddleName, current.Email!.ToUpperInvariant(), null)));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task An_administrator_changes_their_names_and_e_mail()
    {
        var result = await As(_world.Admin).SendAsync(new UpdateProfile.Command(new ProfileUpdateRequest(" Петренко ", "Петро", " ", "Petro@Example.com ", null)));

        Assert.True(result.IsSuccess);
        var admin = Reload(_world.Admin);
        Assert.Equal("Петренко", admin.LastName);
        Assert.Null(admin.MiddleName);
        Assert.Equal("petro@example.com", admin.Email);
        Assert.Equal(admin.Email, admin.UserName);
        Assert.Contains(AuditActions.UserUpdated, _audit.Actions());
    }

    [Fact]
    public async Task The_e_mail_must_be_valid_and_free()
    {
        var admin = As(_world.Admin);

        Assert.Equal(ErrorType.Validation, (await admin.SendAsync(new UpdateProfile.Command(new ProfileUpdateRequest(null, null, null, "not an e-mail", null)))).Failure());
        Assert.Equal(ErrorType.Conflict, (await admin.SendAsync(new UpdateProfile.Command(new ProfileUpdateRequest(null, null, null, _world.EmployeeB.Email, null)))).Failure());
    }

    [Fact]
    public async Task A_refusal_of_the_identity_validators_is_returned()
    {
        _identity.Outcome = Error.Validation("Недопустима пошта.");

        var result = await As(_world.Admin).SendAsync(new UpdateProfile.Command(new ProfileUpdateRequest(null, "Нове", null, null, null)));

        Assert.Equal(ErrorType.Validation, result.Failure());
    }

    [Fact]
    public async Task An_empty_first_name_is_refused()
    {
        var result = await As(_world.Admin).SendAsync(new UpdateProfile.Command(new ProfileUpdateRequest(null, " ", null, null, null)));

        Assert.Equal(ErrorType.Validation, result.Failure());
    }

    [Fact]
    public async Task New_recovery_codes_need_an_enrolled_authenticator_and_its_current_code()
    {
        Assert.Equal(ErrorType.Validation, (await As(_world.EmployeeA).SendAsync(new RegenerateRecoveryCodes.Command("123456"))).Failure());

        _world.EmployeeA.TwoFactorEnabled = true;
        _world.Db.SaveChanges();
        Assert.Equal(ErrorType.Validation, (await As(_world.EmployeeA).SendAsync(new RegenerateRecoveryCodes.Command("000000"))).Failure());
        Assert.Contains(AuditActions.SignInFailed, _audit.Actions());

        var codes = (await As(_world.EmployeeA).SendAsync(new RegenerateRecoveryCodes.Command("123456"))).Ok();
        Assert.Equal(2, codes.Count);
        Assert.Contains(AuditActions.RecoveryCodesGenerated, _audit.Actions());
    }

    [Fact]
    public async Task Signing_out_elsewhere_ends_the_other_sessions_and_is_audited()
    {
        Assert.True((await As(_world.EmployeeA).SendAsync(new SignOutEverywhere.Command())).IsSuccess);

        Assert.Contains($"InvalidateSessions {_world.EmployeeA.Email}", _identity.Calls);
        Assert.Contains(AuditActions.SessionRevoked, _audit.Actions());
    }

    [Fact]
    public async Task A_new_photo_replaces_the_previous_one()
    {
        var first = (await As(_world.EmployeeA).SendAsync(new SetProfilePhoto.Command(Photo(), null))).Ok();
        var second = (await As(_world.EmployeeA).SendAsync(new SetProfilePhoto.Command(Photo(), new ImageCrop(0.1, 0.1, 0.5, 0.5)))).Ok();

        Assert.NotEqual(first, second);
        Assert.Equal(second, Reload(_world.EmployeeA).PhotoId);
        Assert.DoesNotContain(first, _world.Storage.Stored);
        Assert.Contains(second, _world.Storage.Stored);
        Assert.Contains(AuditActions.ProfilePhotoChanged, _audit.Actions());
    }

    [Fact]
    public async Task A_file_the_image_processor_cannot_read_is_refused()
    {
        var result = await As(_world.EmployeeA).SendAsync(new SetProfilePhoto.Command(new FileUpload(new MemoryStream([0, 1, 2, 3]), "notes.png", "image/png", 4), null));

        Assert.Equal(ErrorType.Validation, result.Failure());
        Assert.Null(Reload(_world.EmployeeA).PhotoId);
    }

    /// <summary>
    /// A small PNG file.
    /// </summary>
    /// <returns>The upload.</returns>
    private static FileUpload Photo() => new(new MemoryStream([0x89, 0x50, 0x4E, 0x47, 1, 2, 3]), "me.png", "image/png", 7);

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
    /// Reads a user again from the database.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>The current state.</returns>
    private User Reload(User user) => _world.Master.Users.Single(u => u.Id == user.Id);
}
