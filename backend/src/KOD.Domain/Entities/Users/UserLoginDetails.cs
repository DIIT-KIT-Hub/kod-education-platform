namespace KOD.Domain.Entities.Users;

/// <summary>
/// Represents user login-related details, including credentials status
/// and authorization data such as role and permissions.
/// </summary>
public sealed class UserLoginDetails
{
    /// <summary>
    /// The unique identifier of the user.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The hashed password of the user.
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>
    /// Indicates whether the user's email has been confirmed.
    /// </summary>
    public bool EmailConfirmed { get; set; }

    /// <summary>
    /// The role assigned to the user (e.g. Admin, User).
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// The collection of permissions granted to the user.
    /// </summary>
    public IEnumerable<string>? Permissions { get; set; }
}
