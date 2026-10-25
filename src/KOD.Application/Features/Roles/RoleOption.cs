namespace KOD.Application.Features.Roles;

/// <summary>
/// A role option for select inputs.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Code">The code.</param>
/// <param name="Name">The name.</param>
public sealed record RoleOption(Guid Id, string Code, string Name);
