namespace ODC.Web.Features.Admin.Audit;

/// <summary>
/// A row of the audit trail.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="OccurredAt">The moment of the action.</param>
/// <param name="Action">The action code.</param>
/// <param name="UserId">The acting user identifier.</param>
/// <param name="UserName">The acting user name.</param>
/// <param name="EntityType">The affected entity type.</param>
/// <param name="EntityId">The affected entity identifier.</param>
/// <param name="Details">The description.</param>
/// <param name="IpAddress">The client IP address.</param>
/// <param name="UserPhotoId">The current profile photo of the user.</param>
/// <param name="OrganizationCode">The organization whose trail holds the entry, or <see langword="null"/> for the
/// platform's trail (accounts, catalogs, requests to the support).</param>
public sealed record AuditEntryDto(
    Guid Id,
    DateTimeOffset OccurredAt,
    string Action,
    Guid? UserId,
    string? UserName,
    string? EntityType,
    string? EntityId,
    string? Details,
    string? IpAddress,
    Guid? UserPhotoId,
    string? OrganizationCode);
