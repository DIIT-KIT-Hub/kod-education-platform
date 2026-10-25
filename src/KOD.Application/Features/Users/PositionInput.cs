namespace KOD.Application.Features.Users;

/// <summary>
/// A position to assign to a user.
/// </summary>
/// <param name="UnitId">The unit identifier.</param>
/// <param name="RoleId">The role identifier.</param>
/// <param name="Title">The official position title.</param>
/// <param name="IsPrimary">Whether this is the main position.</param>
public sealed record PositionInput(Guid UnitId, Guid RoleId, string? Title, bool IsPrimary);
