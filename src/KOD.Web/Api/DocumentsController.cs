using KOD.Application.Common.Files;
using KOD.Application.Common.Paging;
using KOD.Application.Common.Results;
using KOD.Application.Features.Documents;
using KOD.Domain.Access;
using KOD.Domain.Documents;
using KOD.Web.Api.Contracts;
using KOD.Web.Security;
using Microsoft.AspNetCore.Mvc;

namespace KOD.Web.Api;

/// <summary>
/// Documents, their files, signatures and rejections.
/// </summary>
/// <param name="documents">The document service.</param>
[Route("api/v1/documents")]
[RequirePermission(Permissions.Documents.View)]
public sealed class DocumentsController(IDocumentService documents) : ApiControllerBase
{
    /// <summary>
    /// Lists documents visible to the current user.
    /// </summary>
    /// <param name="query">Paging and filters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A page of documents.</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResult<DocumentListItem>>> List([FromQuery] DocumentQuery query, CancellationToken cancellationToken)
        => FromResult(await documents.ListAsync(query, cancellationToken));

    /// <summary>
    /// Gets a document with its signing route and signatures.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The document.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentDetails>> Get(Guid id, CancellationToken cancellationToken)
        => FromResult(await documents.GetAsync(id, cancellationToken));

    /// <summary>
    /// Creates a document (multipart form with the file).
    /// </summary>
    /// <param name="form">The form.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>201 with the location of the new document.</returns>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FileRules.MaxDocumentSize + (1024 * 1024))]
    [RequirePermission(Permissions.Documents.Create)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult> Create([FromForm] CreateDocumentForm form, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(form);

        await using var content = form.File!.OpenReadStream();
        var result = await documents.CreateAsync(
            new CreateDocumentCommand(
                form.Title,
                form.Description,
                form.Kind,
                form.OrganizationUnitId,
                form.SigningMode,
                form.SignerIds,
                form.Submit,
                new FileUpload(content, form.File.FileName, form.File.ContentType, form.File.Length)),
            cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value }, new { id = result.Value })
            : ProblemFor(result.Error);
    }

    /// <summary>
    /// Changes a draft or rejected document (author only). Sending a new file removes every signature; removing a signer
    /// from the route removes that signer's signature.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="form">The form; without a file the current file is kept.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpPut("{id:guid}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FileRules.MaxDocumentSize + (1024 * 1024))]
    [RequirePermission(Permissions.Documents.Create)]
    public async Task<ActionResult> Update(Guid id, [FromForm] UpdateDocumentForm form, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(form);

        await using var content = form.File?.OpenReadStream();
        var upload = form.File is null ? null : new FileUpload(content!, form.File.FileName, form.File.ContentType, form.File.Length);
        return FromResult(await documents.UpdateAsync(
            new UpdateDocumentCommand(id, form.Title, form.Description, form.Kind, form.OrganizationUnitId, form.SigningMode, form.SignerIds, form.Submit, upload),
            cancellationToken));
    }

    /// <summary>
    /// Changes the life-cycle state: submit for signing, withdraw to drafts or archive.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="request">The target state.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult> SetStatus(Guid id, DocumentStatusRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = request.Status switch
        {
            DocumentStatus.InSigning => await documents.SubmitAsync(id, cancellationToken),
            DocumentStatus.Draft => await documents.WithdrawAsync(id, cancellationToken),
            DocumentStatus.Archived => await documents.ArchiveAsync(id, cancellationToken),
            _ => Result.Failure(Error.Validation("Недопустимий перехід стану.")),
        };

        return FromResult(result);
    }

    /// <summary>
    /// Deletes a document without signatures.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => FromResult(await documents.DeleteAsync(id, cancellationToken));

    /// <summary>
    /// Downloads the document file (the browser signs exactly these bytes).
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The file.</returns>
    [HttpGet("{id:guid}/file")]
    [Produces("application/octet-stream")]
    public async Task<ActionResult> File(Guid id, CancellationToken cancellationToken)
        => FromFile(await documents.OpenFileAsync(id, cancellationToken));

    /// <summary>
    /// Lists the signatures of a document.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The signatures.</returns>
    [HttpGet("{id:guid}/signatures")]
    public async Task<ActionResult<IReadOnlyList<DocumentSignatureItem>>> Signatures(Guid id, CancellationToken cancellationToken)
    {
        var document = await documents.GetAsync(id, cancellationToken);
        return document.IsSuccess ? Ok(document.Value.Signatures) : ProblemFor(document.Error);
    }

    /// <summary>
    /// Attaches a qualified electronic signature created in the signer's browser.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="form">The signature and its metadata.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>201 with the location of the signature file.</returns>
    [HttpPost("{id:guid}/signatures")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FileRules.MaxSignatureSize + (1024 * 1024))]
    [RequirePermission(Permissions.Documents.Sign)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult> AddSignature(Guid id, [FromForm] SignatureUploadForm form, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(form);

        await using var content = form.Signature!.OpenReadStream();
        var result = await documents.AddSignatureAsync(
            new AddSignatureCommand(
                id,
                form.Format,
                form.Packaging,
                form.Level,
                form.KeyMedia,
                form.SignedAt,
                new SignerCertificate(form.SubjectFullName, form.SubjectOrganization, form.SubjectTaxId, form.IssuerName, form.SerialNumber, form.ValidFrom, form.ValidTo),
                new FileUpload(content, form.Signature.FileName, form.Signature.ContentType, form.Signature.Length)),
            cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(SignatureFile), new { id, signatureId = result.Value }, new { id = result.Value })
            : ProblemFor(result.Error);
    }

    /// <summary>
    /// Downloads a signature file.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="signatureId">The signature identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The file.</returns>
    [HttpGet("{id:guid}/signatures/{signatureId:guid}/file")]
    [Produces("application/octet-stream")]
    public async Task<ActionResult> SignatureFile(Guid id, Guid signatureId, CancellationToken cancellationToken)
        => FromFile(await documents.OpenSignatureAsync(id, signatureId, cancellationToken));

    /// <summary>
    /// Declines to sign the document.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="request">The rejection reason.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>201 on success.</returns>
    [HttpPost("{id:guid}/rejections")]
    [RequirePermission(Permissions.Documents.Sign)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult> Reject(Guid id, RejectionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await documents.RejectAsync(id, request.Reason, cancellationToken);
        return result.IsSuccess ? CreatedAtAction(nameof(Get), new { id }, null) : ProblemFor(result.Error);
    }
}
