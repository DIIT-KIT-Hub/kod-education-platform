namespace KOD.Application.Common.Files;

/// <summary>
/// A file received from a client.
/// </summary>
/// <param name="Content">The content stream (owned by the caller).</param>
/// <param name="FileName">The client-provided file name.</param>
/// <param name="ContentType">The client-provided MIME type.</param>
/// <param name="Length">The declared length in bytes.</param>
public sealed record FileUpload(Stream Content, string FileName, string ContentType, long Length);
