using KOD.Application.Common.Files;
using KOD.Domain.Documents;

namespace KOD.Application.Features.Documents;

/// <summary>
/// Changes a draft or a rejected document: details, signing route and, optionally, the file.
/// </summary>
/// <param name="Id">The document identifier.</param>
/// <param name="Title">The title.</param>
/// <param name="Description">The description.</param>
/// <param name="Kind">The kind.</param>
/// <param name="OrganizationUnitId">The unit the document belongs to.</param>
/// <param name="SigningMode">The signing order.</param>
/// <param name="SignerIds">The signers in signing order.</param>
/// <param name="Submit">Whether to send the document for signing after saving.</param>
/// <param name="File">A new file, or <see langword="null"/> to keep the current one.</param>
public sealed record UpdateDocumentCommand(
    Guid Id,
    string Title,
    string? Description,
    DocumentKind Kind,
    Guid OrganizationUnitId,
    SigningMode SigningMode,
    IReadOnlyList<Guid> SignerIds,
    bool Submit,
    FileUpload? File);
