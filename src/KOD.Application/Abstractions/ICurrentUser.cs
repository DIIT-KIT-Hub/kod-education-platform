namespace KOD.Application.Abstractions;

/// <summary>
/// The user on whose behalf the current request or interactive circuit is executed.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// Gets a value indicating whether the user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the user identifier, or <see langword="null"/> for anonymous users.
    /// </summary>
    Guid? Id { get; }

    /// <summary>
    /// Gets the display name of the user.
    /// </summary>
    string? DisplayName { get; }

    /// <summary>
    /// Gets the client IP address of the current request, if known.
    /// </summary>
    string? IpAddress { get; }

    /// <summary>
    /// Gets the effective permissions of the user.
    /// </summary>
    IReadOnlySet<string> Permissions { get; }

    /// <summary>
    /// Determines whether the user has the given permission.
    /// </summary>
    /// <param name="permission">The permission code.</param>
    /// <returns><see langword="true"/> when the permission is granted.</returns>
    bool Has(string permission);
}
