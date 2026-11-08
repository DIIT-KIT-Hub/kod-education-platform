using ODC.Web.Features.Documents.Domain;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Files;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Documents;

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
[RequirePermission(Permissions.Documents.Create)]
public sealed record CreateDocumentCommand(
    string Title,
    string? Description,
    DocumentKind Kind,
    Guid OrganizationUnitId,
    SigningMode SigningMode,
    IReadOnlyList<Guid> SignerIds,
    bool Submit,
    FileUpload File) : ICommand<Result<Guid>>;
