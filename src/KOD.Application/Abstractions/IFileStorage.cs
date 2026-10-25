namespace KOD.Application.Abstractions;

/// <summary>
/// Private storage for documents and signatures. Files are never served directly by the web server.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Stores the content and computes its size and SHA-256 digest while streaming.
    /// </summary>
    /// <param name="content">The content stream.</param>
    /// <param name="category">The logical folder (e.g. <c>documents</c>, <c>signatures</c>).</param>
    /// <param name="maxSize">The maximum allowed size in bytes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The stored blob information.</returns>
    Task<StoredBlob> SaveAsync(Stream content, string category, long maxSize, CancellationToken cancellationToken);

    /// <summary>
    /// Opens a stored file for reading.
    /// </summary>
    /// <param name="storageKey">The storage key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The readable stream; the caller disposes it.</returns>
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a stored file if it exists.
    /// </summary>
    /// <param name="storageKey">The storage key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the file is deleted.</returns>
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}
