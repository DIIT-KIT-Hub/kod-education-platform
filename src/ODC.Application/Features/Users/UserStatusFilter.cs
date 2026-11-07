namespace ODC.Application.Features.Users;

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

    /// <summary>Active accounts waiting for their owner to activate them (no password yet).</summary>
    AwaitingActivation = 3,
}
