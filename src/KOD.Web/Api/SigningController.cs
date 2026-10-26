using KOD.Application.Features.Signing;
using Microsoft.AspNetCore.Mvc;

namespace KOD.Web.Api;

/// <summary>
/// The file signing tool's journal: the browser reports what it signed (names and parameters, never the content).
/// </summary>
/// <param name="journal">The signing journal.</param>
[Route("api/v1/signing")]
public sealed class SigningController(ISigningJournal journal) : ApiControllerBase
{
    /// <summary>
    /// Records that the current user signed files with the file signing tool.
    /// </summary>
    /// <param name="record">The file names and signature parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpPost("journal")]
    public async Task<ActionResult> Record(FileSigningRecord record, CancellationToken cancellationToken)
        => FromResult(await journal.RecordFilesSignedAsync(record, cancellationToken));
}
