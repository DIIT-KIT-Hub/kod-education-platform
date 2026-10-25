namespace KOD.Application.Common.Models;

/// <summary>
/// A lightweight reference to a unit type.
/// </summary>
/// <param name="Id">The type identifier.</param>
/// <param name="Name">The user-facing name.</param>
/// <param name="Icon">The icon name.</param>
/// <param name="IsOrganization">Whether units of this type are organizations.</param>
public sealed record UnitTypeRef(Guid Id, string Name, string Icon, bool IsOrganization);
