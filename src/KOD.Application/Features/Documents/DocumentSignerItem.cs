using KOD.Domain.Documents;

namespace KOD.Application.Features.Documents;

/// <summary>
/// A step of the signing route.
/// </summary>
/// <param name="UserId">The signer's user identifier.</param>
/// <param name="FullName">The signer's full name.</param>
/// <param name="Position">The signer's primary position.</param>
/// <param name="Order">The 1-based position in the route.</param>
/// <param name="Status">The decision state.</param>
/// <param name="DecidedAt">The decision moment.</param>
/// <param name="Comment">The comment (rejection reason).</param>
/// <param name="PhotoId">The signer's profile photo.</param>
public sealed record DocumentSignerItem(
    Guid UserId,
    string FullName,
    string? Position,
    int Order,
    SignerStatus Status,
    DateTimeOffset? DecidedAt,
    string? Comment,
    Guid? PhotoId);
