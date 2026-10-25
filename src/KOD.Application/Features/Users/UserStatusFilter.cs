namespace KOD.Application.Features.Users;

/// <summary>
/// Account state filter.
/// </summary>
public enum UserStatusFilter
{
    /// <summary>All accounts.</summary>
    All = 0,

    /// <summary>Active accounts.</summary>
    Active = 1,

    /// <summary>Blocked accounts.</summary>
    Blocked = 2,
}
