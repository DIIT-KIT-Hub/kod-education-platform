using Microsoft.AspNetCore.Http.HttpResults;
using ODC.Web.Endpoints;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Signing;

/// <summary>
/// The file signing tool's journal: the browser reports what it signed (names and parameters, never the content). Files
/// are signed in the browser and never reach the server, so the audit entry is the only server-side trace.
/// </summary>
public static class RecordFilesSigned
{
    /// <summary>
    /// The most files one signature may cover (the signing tool allows fewer).
    /// </summary>
    private const int MaxFiles = 50;

    /// <summary>
    /// The longest file name kept in the log.
    /// </summary>
    private const int MaxNameLength = 255;

    /// <summary>
    /// Writes the audit entry.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="audit">The audit logger.</param>
    internal sealed class Handler(ApplicationDbContext db, IAuditLogger audit) : IRequestHandler<FileSigningRecord, Result>
    {
        /// <inheritdoc />
        public async Task<Result> HandleAsync(FileSigningRecord request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var names = request.FileNames
                .Select(static n => Path.GetFileName(n?.Trim() ?? string.Empty))
                .Where(static n => n.Length > 0)
                .Select(static n => n.Length > MaxNameLength ? n[..MaxNameLength] : n)
                .ToList();
            if (names.Count is 0 or > MaxFiles)
            {
                return Error.Validation(Texts.T("Некоректний перелік підписаних файлів."));
            }

            // The details are stored as written (like every audit entry); the certificate data identifies the key used.
            var parameters = string.IsNullOrWhiteSpace(request.Level) ? request.Format.ToString() : $"{request.Format} {request.Level.Trim()}";
            var signer = string.Join(", ", new[] { request.SignerName, request.Issuer, request.CertificateSerial is { } serial ? $"№ {serial}" : null }
                .Where(static p => !string.IsNullOrWhiteSpace(p))
                .Select(static p => p!.Trim()));
            audit.Add(AuditActions.FilesSigned, details: $"{string.Join(", ", names)}: {parameters}{(signer.Length > 0 ? $"; {signer}" : string.Empty)}");
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }

    /// <summary>
    /// <c>POST /api/v1/signing/journal</c>.
    /// </summary>
    internal sealed class Endpoint : IEndpoint
    {
        /// <inheritdoc />
        public void Map(IEndpointRouteBuilder api) => api.MapPost("signing/journal", HandleAsync).WithTags("Signing");

        /// <summary>
        /// Records that the current user signed files with the file signing tool.
        /// </summary>
        /// <param name="record">The file names and signature parameters.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>204 on success.</returns>
        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
            FileSigningRecord record,
            IRequestHandler<FileSigningRecord, Result> handler,
            CancellationToken cancellationToken)
            => (await handler.HandleAsync(record, cancellationToken)).ToNoContent();
    }
}
