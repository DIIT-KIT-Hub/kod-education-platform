namespace KOD.Application.Common.Models;

/// <summary>
/// A reference to a user for display.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="FullName">The full name.</param>
/// <param name="PhotoId">The profile photo, if uploaded.</param>
public sealed record UserRef(Guid Id, string FullName, Guid? PhotoId);
