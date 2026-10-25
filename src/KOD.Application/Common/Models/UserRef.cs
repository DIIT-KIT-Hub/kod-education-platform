namespace KOD.Application.Common.Models;

/// <summary>
/// A lightweight reference to a user.
/// </summary>
/// <param name="Id">The user identifier.</param>
/// <param name="FullName">The full name.</param>
public sealed record UserRef(Guid Id, string FullName);
