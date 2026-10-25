namespace KOD.Application.Features.Users;

/// <summary>
/// A row of the user grid.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="FullName">The full name.</param>
/// <param name="Email">The e-mail address.</param>
/// <param name="Position">The primary position title.</param>
/// <param name="Unit">The primary unit name.</param>
/// <param name="IsActive">Whether the account is active.</param>
/// <param name="TwoFactorEnabled">Whether the authenticator is enrolled.</param>
/// <param name="IsLockedOut">Whether the account is temporarily locked out.</param>
/// <param name="LastSignInAt">The last sign-in moment.</param>
public sealed record UserListItem(
    Guid Id,
    string FullName,
    string Email,
    string? Position,
    string? Unit,
    bool IsActive,
    bool TwoFactorEnabled,
    bool IsLockedOut,
    DateTimeOffset? LastSignInAt);
