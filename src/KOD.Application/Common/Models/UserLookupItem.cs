namespace KOD.Application.Common.Models;

/// <summary>
/// A user suggestion for pickers (e.g. adding signers).
/// </summary>
/// <param name="Id">The user identifier.</param>
/// <param name="FullName">The full name.</param>
/// <param name="Email">The e-mail address.</param>
/// <param name="Position">The primary position description.</param>
/// <param name="PhotoId">The profile photo, if uploaded.</param>
public sealed record UserLookupItem(Guid Id, string FullName, string Email, string? Position, Guid? PhotoId);
