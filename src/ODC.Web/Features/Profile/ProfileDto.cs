using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Models;

namespace ODC.Web.Features.Profile;

/// <summary>
/// The current user's profile.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="FullName">The full name.</param>
/// <param name="LastName">The last name.</param>
/// <param name="FirstName">The first name.</param>
/// <param name="MiddleName">The patronymic.</param>
/// <param name="Email">The e-mail address.</param>
/// <param name="PhoneNumber">The phone number.</param>
/// <param name="Positions">The positions.</param>
/// <param name="Modules">The modules available to the user.</param>
/// <param name="Permissions">The effective permissions.</param>
/// <param name="TwoFactorEnabled">Whether the authenticator is enrolled.</param>
/// <param name="RecoveryCodesLeft">The number of unused recovery codes.</param>
/// <param name="CreatedAt">The account creation moment.</param>
/// <param name="LastSignInAt">The last sign-in moment.</param>
/// <param name="Language">The chosen interface language, or <see langword="null"/> when not chosen.</param>
/// <param name="PhotoId">The profile photo, if uploaded.</param>
/// <param name="CanEditPersonalData">Whether the user may change own name and e-mail: an administrator of users whose
/// scope covers the whole account (a system administrator always); the others ask their administrator or HR.</param>
public sealed record ProfileDto(
    Guid Id,
    string FullName,
    string LastName,
    string FirstName,
    string? MiddleName,
    string Email,
    string? PhoneNumber,
    IReadOnlyList<PositionDto> Positions,
    IReadOnlyList<AppModule> Modules,
    IReadOnlyList<PermissionDefinition> Permissions,
    bool TwoFactorEnabled,
    int RecoveryCodesLeft,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSignInAt,
    string? Language,
    Guid? PhotoId,
    bool CanEditPersonalData);
