using KOD.Domain.Common;

namespace KOD.Domain.Access;

/// <summary>
/// A role (position template) that grants a set of permissions to staff assigned to it.
/// </summary>
public sealed class Role : AuditableEntity
{
    /// <summary>
    /// Maximum length of <see cref="Code"/>.
    /// </summary>
    public const int CodeMaxLength = 64;

    /// <summary>
    /// Maximum length of <see cref="Name"/>.
    /// </summary>
    public const int NameMaxLength = 128;

    /// <summary>
    /// Maximum length of <see cref="Description"/>.
    /// </summary>
    public const int DescriptionMaxLength = 512;

    /// <summary>
    /// The permissions granted by the role.
    /// </summary>
    private readonly List<RolePermission> _permissions = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="Role"/> class.
    /// </summary>
    /// <param name="code">The unique role code.</param>
    /// <param name="name">The user-facing name.</param>
    /// <param name="description">The user-facing description.</param>
    /// <param name="isSystem">Whether the role is predefined and cannot be deleted.</param>
    public Role(string code, string name, string? description, bool isSystem)
    {
        Code = Guard.Text(code, CodeMaxLength, "Код").ToLowerInvariant();
        Name = Guard.Text(name, NameMaxLength, "Назва");
        Description = Guard.OptionalText(description, DescriptionMaxLength, "Опис");
        IsSystem = isSystem;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Role"/> class for EF Core materialization.
    /// </summary>
    private Role()
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    /// <summary>
    /// Gets the unique role code used in imports and seeding.
    /// </summary>
    public string Code { get; private set; }

    /// <summary>
    /// Gets the user-facing name.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the user-facing description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the role is predefined (cannot be deleted or renamed by code).
    /// </summary>
    public bool IsSystem { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the role is the super-user role that implicitly owns every permission.
    /// </summary>
    public bool IsAdministrator => Code == SystemRoles.Administrator;

    /// <summary>
    /// Gets the permissions granted by the role.
    /// </summary>
    public IReadOnlyCollection<RolePermission> Permissions => _permissions;

    /// <summary>
    /// Updates the user-facing data of the role.
    /// </summary>
    /// <param name="name">The new name.</param>
    /// <param name="description">The new description.</param>
    public void Update(string name, string? description)
    {
        Name = Guard.Text(name, NameMaxLength, "Назва");
        Description = Guard.OptionalText(description, DescriptionMaxLength, "Опис");
    }

    /// <summary>
    /// Replaces the permissions of the role.
    /// </summary>
    /// <param name="permissions">The permission codes to grant.</param>
    public void SetPermissions(IEnumerable<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        var requested = permissions.ToHashSet(StringComparer.Ordinal);
        if (IsAdministrator)
        {
            requested = [.. KOD.Domain.Access.Permissions.Codes];
        }

        var unknown = requested.FirstOrDefault(static p => !KOD.Domain.Access.Permissions.Codes.Contains(p));
        if (unknown is not null)
        {
            throw new DomainException($"Невідомий дозвіл «{unknown}».");
        }

        _permissions.RemoveAll(p => !requested.Contains(p.Permission));
        foreach (var code in requested.Where(code => _permissions.TrueForAll(p => p.Permission != code)))
        {
            _permissions.Add(new RolePermission(Id, code));
        }
    }
}
