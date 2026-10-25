namespace KOD.Application.Features.Organization;

/// <summary>
/// A staff member of a unit.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="FullName">The full name.</param>
/// <param name="Position">The position title.</param>
/// <param name="Email">The e-mail address.</param>
/// <param name="PhoneNumber">The phone number.</param>
/// <param name="IsPrimary">Whether this is the main position of the user.</param>
/// <param name="IsLeadership">Whether the position's role is a leadership position.</param>
public sealed record UnitStaffItem(Guid UserId, string FullName, string Position, string Email, string? PhoneNumber, bool IsPrimary, bool IsLeadership);
