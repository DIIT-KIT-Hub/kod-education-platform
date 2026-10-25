namespace KOD.Domain.Access;

/// <summary>
/// Describes a single permission of the system.
/// </summary>
/// <param name="Code">The stable machine-readable code (e.g. <c>documents.sign</c>).</param>
/// <param name="Module">The module the permission belongs to.</param>
/// <param name="Title">The short user-facing title.</param>
/// <param name="Description">The user-facing explanation of what the permission allows.</param>
public sealed record PermissionDefinition(string Code, AppModule Module, string Title, string Description);
