using ODC.Web.Features.Documents.Domain;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Files;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Documents;

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
[RequirePermission(Permissions.Documents.Create)]
public sealed record UpdateDocumentCommand(
    Guid Id,
    string Title,
    string? Description,
    DocumentKind Kind,
    Guid OrganizationUnitId,
    SigningMode SigningMode,
    IReadOnlyList<Guid> SignerIds,
    bool Submit,
    FileUpload? File) : ICommand<Result>;
