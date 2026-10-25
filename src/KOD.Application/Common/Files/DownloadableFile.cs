namespace KOD.Application.Common.Files;

/// <summary>
/// A file returned to a client.
/// </summary>
/// <param name="Content">The content stream; the receiver disposes it.</param>
/// <param name="FileName">The suggested file name.</param>
/// <param name="ContentType">The MIME type.</param>
public sealed record DownloadableFile(Stream Content, string FileName, string ContentType);
