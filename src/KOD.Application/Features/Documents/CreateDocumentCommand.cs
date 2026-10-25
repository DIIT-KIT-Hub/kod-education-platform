using KOD.Application.Common.Files;
using KOD.Domain.Documents;

namespace KOD.Application.Features.Documents;

/// <summary>
/// Creates a document with its signing route.
/// </summary>
/// <param name="Title">The title.</param>
/// <param name="Description">The description.</param>
/// <param name="Kind">The kind.</param>
/// <param name="OrganizationUnitId">The unit the document belongs to.</param>
/// <param name="SigningMode">The signing order.</param>
/// <param name="SignerIds">The signers in signing order.</param>
/// <param name="Submit">Whether to send the document for signing immediately.</param>
/// <param name="File">The document file.</param>
public sealed record CreateDocumentCommand(
    string Title,
    string? Description,
    DocumentKind Kind,
    Guid OrganizationUnitId,
    SigningMode SigningMode,
    IReadOnlyList<Guid> SignerIds,
    bool Submit,
    FileUpload File);
