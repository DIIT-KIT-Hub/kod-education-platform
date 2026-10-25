using KOD.Application.Common.Models;
using KOD.Domain.Access;

namespace KOD.Application.Features.Profile;

/// <summary>
/// The current user's profile.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="FullName">The full name.</param>
/// <param name="Email">The e-mail address.</param>
/// <param name="PhoneNumber">The phone number.</param>
/// <param name="Positions">The positions.</param>
/// <param name="Modules">The modules available to the user.</param>
/// <param name="Permissions">The effective permissions.</param>
/// <param name="TwoFactorEnabled">Whether the authenticator is enrolled.</param>
/// <param name="RecoveryCodesLeft">The number of unused recovery codes.</param>
/// <param name="CreatedAt">The account creation moment.</param>
/// <param name="LastSignInAt">The last sign-in moment.</param>
public sealed record ProfileDto(
    Guid Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    IReadOnlyList<PositionDto> Positions,
    IReadOnlyList<AppModule> Modules,
    IReadOnlyList<PermissionDefinition> Permissions,
    bool TwoFactorEnabled,
    int RecoveryCodesLeft,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSignInAt);
