using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Features.Documents.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Files;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Documents;

/// <summary>
/// The files of a visible document: the document itself (the browser signs exactly these bytes) and its signatures.
/// </summary>
public static class DownloadDocumentFiles
{
    /// <summary>
    /// The document file.
    /// </summary>
    /// <param name="Id">The document.</param>
    public sealed record FileQuery(Guid Id) : IQuery<Result<DownloadableFile>>;

    /// <summary>
    /// A signature file, named after the document at download time (signatures stored under older names download the
    /// same way).
    /// </summary>
    /// <param name="Id">The document.</param>
    /// <param name="SignatureId">The signature.</param>
    public sealed record SignatureQuery(Guid Id, Guid SignatureId) : IQuery<Result<DownloadableFile>>;

    /// <summary>
    /// Opens the document file.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="desk">The shared document rules.</param>
    /// <param name="fileStorage">The private file storage.</param>
    internal sealed class FileHandler(ApplicationDbContext db, DocumentDesk desk, IFileStorage fileStorage) : IRequestHandler<FileQuery, Result<DownloadableFile>>
    {
        /// <inheritdoc />
        public async Task<Result<DownloadableFile>> HandleAsync(FileQuery request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (!await desk.IsVisibleAsync(request.Id, cancellationToken))
            {
                return DocumentDesk.NotFound();
            }

            var file = await db.Documents.AsNoTracking().Where(d => d.Id == request.Id).Select(static d => d.File).FirstAsync(cancellationToken);
            var stream = await fileStorage.OpenReadAsync(file.StorageKey, cancellationToken);
            return new DownloadableFile(stream, file.FileName, file.ContentType);
        }
    }

    /// <summary>
    /// Opens a signature file.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="desk">The shared document rules.</param>
    /// <param name="fileStorage">The private file storage.</param>
    internal sealed class SignatureHandler(ApplicationDbContext db, DocumentDesk desk, IFileStorage fileStorage) : IRequestHandler<SignatureQuery, Result<DownloadableFile>>
    {
        /// <inheritdoc />
        public async Task<Result<DownloadableFile>> HandleAsync(SignatureQuery request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var (id, signatureId) = (request.Id, request.SignatureId);
            if (!await desk.IsVisibleAsync(id, cancellationToken))
            {
                return DocumentDesk.NotFound();
            }

            var signature = await db.DocumentSignatures
                .AsNoTracking()
                .Where(s => s.Id == signatureId && s.DocumentId == id)
                .Select(static s => new { s.File, s.Format })
                .FirstOrDefaultAsync(cancellationToken);
            if (signature is null)
            {
                return Error.NotFound(Texts.T("Підпис не знайдено."));
            }

            var documentName = await db.Documents.AsNoTracking().Where(d => d.Id == id).Select(static d => d.File.FileName).FirstAsync(cancellationToken);
            var stream = await fileStorage.OpenReadAsync(signature.File.StorageKey, cancellationToken);
            return new DownloadableFile(stream, SignedFileName.For(documentName, signature.Format), signature.File.ContentType);
        }
    }

    /// <summary>
    /// <c>GET /api/v1/documents/{id}/file</c> and <c>GET /api/v1/documents/{id}/signatures/{signatureId}/file</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api)
        {
            var documents = DocumentDesk.MapDocuments(api);
            documents.MapGet("{id:guid}/file", FileAsync).Produces(StatusCodes.Status200OK, contentType: "application/octet-stream");
            documents.MapGet("{id:guid}/signatures/{signatureId:guid}/file", SignatureAsync).Produces(StatusCodes.Status200OK, contentType: "application/octet-stream");
        }

        /// <summary>
        /// Downloads the document file (the browser signs exactly these bytes).
        /// </summary>
        /// <param name="id">The document.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The file.</returns>
        private static async Task<Results<FileStreamHttpResult, ProblemHttpResult>> FileAsync(Guid id, IRequestHandler<FileQuery, Result<DownloadableFile>> handler, CancellationToken cancellationToken)
            => (await handler.HandleAsync(new FileQuery(id), cancellationToken)).ToFile();

        /// <summary>
        /// Downloads a signature file.
        /// </summary>
        /// <param name="id">The document.</param>
        /// <param name="signatureId">The signature.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The signature file.</returns>
        private static async Task<Results<FileStreamHttpResult, ProblemHttpResult>> SignatureAsync(
            Guid id,
            Guid signatureId,
            IRequestHandler<SignatureQuery, Result<DownloadableFile>> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(new SignatureQuery(id, signatureId), cancellationToken)).ToFile();
    }
}
