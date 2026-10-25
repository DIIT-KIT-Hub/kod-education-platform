namespace KOD.Application.Common.Models;

/// <summary>
/// A lightweight reference to an organization unit.
/// </summary>
/// <param name="Id">The unit identifier.</param>
/// <param name="Name">The unit name.</param>
public sealed record UnitRef(Guid Id, string Name);
