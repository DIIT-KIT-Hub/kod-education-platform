namespace KOD.Domain.Access;

/// <summary>
/// Functional module of the cabinet. A module is visible to a user when the user has at least one of its permissions.
/// </summary>
public enum AppModule
{
    /// <summary>
    /// Electronic document management: drafts, approval routes, signatures, registry.
    /// </summary>
    Documents = 0,

    /// <summary>
    /// Stand-alone qualified electronic signature tools (sign and verify any file).
    /// </summary>
    Signing = 1,

    /// <summary>
    /// University news and announcements.
    /// </summary>
    News = 2,

    /// <summary>
    /// Organizational structure: institutes, faculties, departments, divisions.
    /// </summary>
    Structure = 3,

    /// <summary>
    /// Staff accounts, positions, import and export.
    /// </summary>
    Users = 4,

    /// <summary>
    /// Roles, permissions and security administration.
    /// </summary>
    Administration = 5,

    /// <summary>
    /// Security and activity audit trail.
    /// </summary>
    Audit = 6,
}
