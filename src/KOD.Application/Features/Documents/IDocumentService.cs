using KOD.Application.Common.Files;
using KOD.Application.Common.Models;
using KOD.Application.Common.Paging;
using KOD.Application.Common.Results;

namespace KOD.Application.Features.Documents;

/// <summary>
/// Electronic document management use cases.
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// Lists documents visible to the current user.
    /// </summary>
    /// <param name="query">The paging and filter parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The page of documents.</returns>
    Task<Result<PagedResult<DocumentListItem>>> ListAsync(DocumentQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a document visible to the current user.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The document.</returns>
    Task<Result<DocumentDetails>> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a document.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The new document identifier.</returns>
    Task<Result<Guid>> CreateAsync(CreateDocumentCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Sends a draft or rejected document for signing.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> SubmitAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a document from signing to drafts.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> WithdrawAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Attaches a browser-created signature.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The new signature identifier.</returns>
    Task<Result<Guid>> AddSignatureAsync(AddSignatureCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Declines to sign a document.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="reason">The reason.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> RejectAsync(Guid id, string reason, CancellationToken cancellationToken);

    /// <summary>
    /// Moves a completed document to the archive.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> ArchiveAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a document without signatures.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Opens the document file.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The file.</returns>
    Task<Result<DownloadableFile>> OpenFileAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Opens a signature file.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="signatureId">The signature identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The file.</returns>
    Task<Result<DownloadableFile>> OpenSignatureAsync(Guid id, Guid signatureId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the units the current user may create documents in.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The units.</returns>
    Task<IReadOnlyList<UnitOption>> GetAuthorUnitsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Searches active users that can be added to a signing route.
    /// </summary>
    /// <param name="term">The search term (name or e-mail).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Up to 10 suggestions.</returns>
    Task<IReadOnlyList<UserLookupItem>> SearchSignersAsync(string term, CancellationToken cancellationToken);
}
