using KOD.Application.Common.Results;

namespace KOD.Application.Features.Signing;

/// <summary>
/// Records the use of the file signing tool in the audit log. Files are signed in the browser and never reach the
/// server, so the journal is the only server-side trace of such a signature.
/// </summary>
public interface ISigningJournal
{
    /// <summary>
    /// Records that the current user signed files.
    /// </summary>
    /// <param name="record">The names and signature parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> RecordFilesSignedAsync(FileSigningRecord record, CancellationToken cancellationToken);
}
