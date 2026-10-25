using KOD.Application.Common.Models;

namespace KOD.Application.Features.Users;

/// <summary>
/// Full information about a user account.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Email">The e-mail address.</param>
/// <param name="LastName">The last name.</param>
/// <param name="FirstName">The first name.</param>
/// <param name="MiddleName">The patronymic.</param>
/// <param name="FullName">The full name.</param>
/// <param name="PhoneNumber">The phone number.</param>
/// <param name="IsActive">Whether the account is active.</param>
/// <param name="TwoFactorEnabled">Whether the authenticator is enrolled.</param>
/// <param name="MustChangePassword">Whether a temporary password is set.</param>
/// <param name="IsLockedOut">Whether the account is temporarily locked out.</param>
/// <param name="CreatedAt">The creation moment.</param>
/// <param name="LastSignInAt">The last sign-in moment.</param>
/// <param name="ActiveSessions">The number of active sessions.</param>
/// <param name="Positions">The positions.</param>
/// <param name="GrantedPermissions">Individually granted permissions.</param>
/// <param name="DeniedPermissions">Individually denied permissions.</param>
/// <param name="EffectivePermissions">The resulting permissions.</param>
public sealed record UserDetails(
    Guid Id,
    string Email,
    string LastName,
    string FirstName,
    string? MiddleName,
    string FullName,
    string? PhoneNumber,
    bool IsActive,
    bool TwoFactorEnabled,
    bool MustChangePassword,
    bool IsLockedOut,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSignInAt,
    int ActiveSessions,
    IReadOnlyList<PositionDto> Positions,
    IReadOnlyList<string> GrantedPermissions,
    IReadOnlyList<string> DeniedPermissions,
    IReadOnlySet<string> EffectivePermissions);
