namespace KOD.Application.Features.Users;

/// <summary>
/// Creates or updates a user account.
/// </summary>
/// <param name="Email">The e-mail address (login).</param>
/// <param name="LastName">The last name.</param>
/// <param name="FirstName">The first name.</param>
/// <param name="MiddleName">The patronymic.</param>
/// <param name="PhoneNumber">The phone number.</param>
/// <param name="Positions">The positions.</param>
/// <param name="GrantedPermissions">Individually granted permissions (applied only for role administrators).</param>
/// <param name="DeniedPermissions">Individually denied permissions (applied only for role administrators).</param>
public sealed record SaveUserCommand(
    string Email,
    string LastName,
    string FirstName,
    string? MiddleName,
    string? PhoneNumber,
    IReadOnlyList<PositionInput> Positions,
    IReadOnlyList<string> GrantedPermissions,
    IReadOnlyList<string> DeniedPermissions);
