namespace KOD.Domain.Entities.Users;

/// <summary>
/// Represents user authentication and authorization information, including role and assigned permissions.
/// </summary>
public sealed class UserAuthInfo
{
    /// <summary>
    /// The unique identifier of the user.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The role assigned to the user (e.g. Admin, User).
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// The collection of permissions granted to the user.
    /// </summary>
    public IEnumerable<string>? Permissions { get; set; }
}
