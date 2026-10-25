namespace KOD.Application.Abstractions;

/// <summary>
/// Issues unique, gap-tolerant registration numbers for documents.
/// </summary>
public interface IDocumentNumberGenerator
{
    /// <summary>
    /// Returns the next registration number (e.g. <c>2026/000123</c>).
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The registration number.</returns>
    Task<string> NextAsync(CancellationToken cancellationToken);
}
