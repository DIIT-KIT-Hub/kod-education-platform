namespace ODC.Web.Shared.Domain.Tenancy;

/// <summary>
/// An account takes part in an organization it does not work in: it signs a document or talks in a chat there as a
/// guest. Kept in the master database next to the memberships, so the lists that gather a person's work from all their
/// organizations (documents waiting for their signature, chats, notifications) know which databases to read, and the
/// person's profile is copied there too. Written before the organization's change it allows, so it may name an
/// organization where nothing is left for the person: a list then finds nothing there.
/// </summary>
public sealed class Participation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Participation"/> class.
    /// </summary>
    /// <param name="userId">The account.</param>
    /// <param name="organizationId">The organization.</param>
    /// <param name="createdAt">When the person was first admitted.</param>
    public Participation(Guid userId, Guid organizationId, DateTimeOffset createdAt)
    {
        UserId = userId;
        OrganizationId = organizationId;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Participation"/> class for EF Core materialization.
    /// </summary>
    private Participation()
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
    /// Gets when the person was first admitted.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private init; }
}
