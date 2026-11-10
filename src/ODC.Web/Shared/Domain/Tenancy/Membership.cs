namespace ODC.Web.Shared.Domain.Tenancy;

/// <summary>
/// An account works in an organization: it holds at least one position there. Kept in the master database so a request
/// can find the organizations of a person without opening their databases; the positions themselves live in the
/// organization's database.
/// </summary>
public sealed class Membership
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Membership"/> class.
    /// </summary>
    /// <param name="userId">The account.</param>
    /// <param name="organizationId">The organization.</param>
    /// <param name="createdAt">When the person joined.</param>
    public Membership(Guid userId, Guid organizationId, DateTimeOffset createdAt)
    {
        UserId = userId;
        OrganizationId = organizationId;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Membership"/> class for EF Core materialization.
    /// </summary>
    private Membership()
    {
    }

    /// <summary>
    /// Gets the account.
    /// </summary>
    public Guid UserId { get; private init; }

    /// <summary>
    /// Gets the organization.
    /// </summary>
    public Guid OrganizationId { get; private init; }

    /// <summary>
    /// Gets when the person joined (the earliest membership is the default organization).
    /// </summary>
    public DateTimeOffset CreatedAt { get; private init; }
}
