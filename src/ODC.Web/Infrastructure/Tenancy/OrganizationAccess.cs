namespace ODC.Web.Infrastructure.Tenancy;

/// <summary>
/// Why the current user may act in the organization of the request.
/// </summary>
public enum OrganizationAccess
{
    /// <summary>
    /// Nobody is signed in (sign-in and activation need no organization).
    /// </summary>
    Anonymous = 0,

    /// <summary>
    /// The person works there (a membership): the permissions of their positions there.
    /// </summary>
    Member = 1,

    /// <summary>
    /// A platform administrator (<c>administration.all</c>): every permission they hold, in every organization.
    /// </summary>
    Platform = 2,

    /// <summary>
    /// Someone from another organization: only what a participant needs (a document to sign, a chat they are in); the
    /// data rules show them nothing else.
    /// </summary>
    Guest = 3,

    /// <summary>
    /// The server itself (seeding, background work, copies of accounts).
    /// </summary>
    System = 4,
}
