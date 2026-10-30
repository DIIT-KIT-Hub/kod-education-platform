using System.Diagnostics.CodeAnalysis;
using KOD.Domain.Common;
using KOD.Domain.Localization;
using Microsoft.AspNetCore.Identity;

namespace KOD.Domain.Access;

/// <summary>
/// A role (position template) that grants a set of permissions to staff assigned to it: an Identity role (table
/// <c>roles</c>) whose <see cref="Name"/> is the unique user-facing name. The permissions are its role claims, the
/// assignments are <see cref="Users.StaffPosition"/> (Identity user roles bound to an organization unit).
/// </summary>
public sealed class Role : IdentityRole<Guid>, IAuditable
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
        Id = Guid.CreateVersion7();
        Code = Guard.Text(code, CodeMaxLength, "Код").ToLowerInvariant();
        Rename(name);
        Description = Guard.OptionalText(description, DescriptionMaxLength, "Опис");
        IsSystem = isSystem;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Role"/> class for EF Core materialization.
    /// </summary>
    private Role()
    {
        Code = string.Empty;
    }

    /// <summary>
    /// Gets the unique role code used in imports and seeding.
    /// </summary>
    public string Code { get; private set; }

    /// <summary>
    /// Gets or sets the unique user-facing name. Use <see cref="Update"/> to change it: the setter exists for the Identity
    /// role store and does not normalize the name.
    /// </summary>
    [AllowNull]
    public override string Name
    {
        get => base.Name ?? string.Empty;
        set => base.Name = value;
    }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Gets the user-facing description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the role is predefined (cannot be deleted or renamed by code).
    /// </summary>
    public bool IsSystem { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the role is a leadership position: its holders are listed first among the staff of a unit.
    /// </summary>
    public bool IsLeadership { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the role is the super-user role that implicitly owns every permission.
    /// </summary>
    public bool IsAdministrator => Code == SystemRoles.Administrator;

    /// <summary>
    /// Gets the permissions granted by the role.
    /// </summary>
    public IReadOnlyCollection<RolePermission> Permissions => _permissions;

    /// <summary>
    /// Normalizes a name the way the Identity lookup normalizer does, so <see cref="RoleManager{TRole}.FindByNameAsync"/>
    /// finds the role.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>The normalized name.</returns>
    public static string NormalizeName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return name.Trim().Normalize().ToUpperInvariant();
    }

    /// <summary>
    /// Updates the user-facing data of the role.
    /// </summary>
    /// <param name="name">The new name.</param>
    /// <param name="description">The new description.</param>
    /// <param name="isLeadership">Whether the role is a leadership position.</param>
    public void Update(string name, string? description, bool isLeadership)
    {
        Rename(name);
        Description = Guard.OptionalText(description, DescriptionMaxLength, "Опис");
        IsLeadership = isLeadership;
        ConcurrencyStamp = Guid.NewGuid().ToString();
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
            throw new DomainException(Texts.T("Невідомий дозвіл «{0}».", unknown));
        }

        _permissions.RemoveAll(p => p.ClaimType != PermissionClaimTypes.Permission || !requested.Contains(p.ClaimValue!));
        foreach (var code in requested.Where(code => _permissions.TrueForAll(p => p.ClaimValue != code)))
        {
            _permissions.Add(new RolePermission(Id, code));
        }
    }

    /// <summary>
    /// Sets the name and its normalized form.
    /// </summary>
    /// <param name="name">The new name.</param>
    private void Rename(string name)
    {
        Name = Guard.Text(name, NameMaxLength, "Назва");
        NormalizedName = NormalizeName(Name);
    }
}
