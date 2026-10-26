using KOD.Domain.Documents;

namespace KOD.Application.Features.Documents;

/// <summary>
/// A row of the document registry.
/// </summary>
/// <param name="Id">The document identifier.</param>
/// <param name="RegistrationNumber">The registration number.</param>
/// <param name="Title">The title.</param>
/// <param name="Kind">The kind.</param>
/// <param name="Status">The life-cycle state.</param>
/// <param name="AuthorName">The author's name.</param>
/// <param name="UnitName">The unit name.</param>
/// <param name="CreatedAt">The creation moment.</param>
/// <param name="SignedCount">The number of collected signatures.</param>
/// <param name="SignersCount">The number of signers on the route.</param>
/// <param name="AwaitingMe">Whether the document waits for the current user's signature.</param>
/// <param name="AuthorPhotoId">The author's profile photo.</param>
public sealed record DocumentListItem(
    Guid Id,
    string RegistrationNumber,
    string Title,
    DocumentKind Kind,
    DocumentStatus Status,
    string AuthorName,
    string UnitName,
    DateTimeOffset CreatedAt,
    int SignedCount,
    int SignersCount,
    bool AwaitingMe,
    Guid? AuthorPhotoId);
