using KOD.Application.Common.Models;
using KOD.Domain.Documents;

namespace KOD.Application.Features.Documents;

/// <summary>
/// Full information about a document.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="RegistrationNumber">The registration number.</param>
/// <param name="Title">The title.</param>
/// <param name="Description">The description.</param>
/// <param name="Kind">The kind.</param>
/// <param name="Status">The life-cycle state.</param>
/// <param name="SigningMode">The signing order.</param>
/// <param name="Author">The author.</param>
/// <param name="Unit">The unit.</param>
/// <param name="File">The file metadata.</param>
/// <param name="CreatedAt">The creation moment.</param>
/// <param name="SubmittedAt">The moment it was sent for signing.</param>
/// <param name="CompletedAt">The moment the route completed.</param>
/// <param name="Signers">The signing route.</param>
/// <param name="Signatures">The collected signatures.</param>
/// <param name="Actions">The actions available to the current user.</param>
/// <param name="CombinedSignatureId">The signature whose file holds the document with every signature (the newest one of a
/// cumulative format), or <see langword="null"/>.</param>
public sealed record DocumentDetails(
    Guid Id,
    string RegistrationNumber,
    string Title,
    string? Description,
    DocumentKind Kind,
    DocumentStatus Status,
    SigningMode SigningMode,
    UserRef Author,
    UnitRef Unit,
    DocumentFileInfo File,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<DocumentSignerItem> Signers,
    IReadOnlyList<DocumentSignatureItem> Signatures,
    DocumentActions Actions,
    Guid? CombinedSignatureId);
