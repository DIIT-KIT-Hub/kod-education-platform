using ODC.Web.Shared.Domain.Tenancy;

namespace ODC.Web.Features.Admin.Organizations;

/// <summary>
/// An organization of the registry as the platform administrator sees it.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Code">The code.</param>
/// <param name="Name">The name.</param>
/// <param name="ShortName">The abbreviation.</param>
/// <param name="State">The state.</param>
/// <param name="DatabaseName">The name of its database.</param>
/// <param name="SchemaVersion">The last migration of its database.</param>
/// <param name="IsCurrent">Whether its database has the latest schema (otherwise it answers no requests).</param>
/// <param name="MigratedAt">When its database was last migrated.</param>
/// <param name="CreatedAt">When it was created.</param>
/// <param name="MemberCount">How many people work there.</param>
/// <param name="OwnRoles">Whether its database has PostgreSQL roles of its own.</param>
public sealed record OrganizationListItem(
    Guid Id,
    string Code,
    string Name,
    string? ShortName,
    OrganizationState State,
    string DatabaseName,
    string? SchemaVersion,
    bool IsCurrent,
    DateTimeOffset? MigratedAt,
    DateTimeOffset CreatedAt,
    int MemberCount,
    bool OwnRoles);
