namespace KOD.Application.Features.Documents;

/// <summary>
/// Public metadata of a document file. The SHA-256 digest lets the browser verify integrity before signing.
/// </summary>
/// <param name="FileName">The file name.</param>
/// <param name="ContentType">The MIME type.</param>
/// <param name="Size">The size in bytes.</param>
/// <param name="Sha256">The lowercase hexadecimal SHA-256 digest.</param>
/// <param name="IsPdf">Whether the file is a PDF (PAdES is available).</param>
public sealed record DocumentFileInfo(string FileName, string ContentType, long Size, string Sha256, bool IsPdf);
