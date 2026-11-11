namespace ODC.Web.Shared.Domain.Tenancy;

/// <summary>
/// Where an organization is in its life: only an active one answers requests.
/// </summary>
public enum OrganizationState
{
    /// <summary>
    /// The registry row is saved; the roles, the database and its first data are being created (a start finishes an
    /// interrupted creation).
    /// </summary>
    Creating,

    /// <summary>
    /// The organization works.
    /// </summary>
    Active,

    /// <summary>
    /// Suspended by the platform: no requests, the data stays.
    /// </summary>
    Suspended,

    /// <summary>
    /// Being deleted: the database, its roles, files and keys go away (a start finishes an interrupted deletion).
    /// </summary>
    Deleting,
}
