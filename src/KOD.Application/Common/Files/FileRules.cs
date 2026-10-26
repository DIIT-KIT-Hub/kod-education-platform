using System.Collections.Frozen;

namespace KOD.Application.Common.Files;

/// <summary>
/// Size and type limits of uploaded files.
/// </summary>
public static class FileRules
{
    /// <summary>
    /// Maximum size of a document file (25 MB): the browser signing library keeps the whole file in its fixed-size heap.
    /// </summary>
    public const long MaxDocumentSize = 25L * 1024 * 1024;

    /// <summary>
    /// Maximum size of a signature file; enveloped formats contain the document itself (40 MB).
    /// </summary>
    public const long MaxSignatureSize = 40L * 1024 * 1024;

    /// <summary>
    /// Maximum size of a user import spreadsheet (5 MB).
    /// </summary>
    public const long MaxImportSize = 5L * 1024 * 1024;

    /// <summary>
    /// Maximum size of an uploaded photo before optimization (15 MB): news images, covers and profile photos.
    /// </summary>
    public const long MaxImageSize = 15L * 1024 * 1024;

    /// <summary>
    /// The image types accepted by photo inputs (the server checks the content, not the type).
    /// </summary>
    public const string ImageAccept = "image/jpeg,image/png,image/webp,image/avif,image/gif";

    /// <summary>
    /// Gets the allowed document extensions mapped to their canonical MIME types.
    /// </summary>
    public static FrozenDictionary<string, string> DocumentTypes { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xls"] = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".ppt"] = "application/vnd.ms-powerpoint",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        [".odt"] = "application/vnd.oasis.opendocument.text",
        [".ods"] = "application/vnd.oasis.opendocument.spreadsheet",
        [".rtf"] = "application/rtf",
        [".txt"] = "text/plain",
        [".xml"] = "application/xml",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".zip"] = "application/zip",
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the value for the HTML <c>accept</c> attribute of document inputs.
    /// </summary>
    public static string DocumentAccept { get; } = string.Join(',', DocumentTypes.Keys.Order(StringComparer.Ordinal));

    /// <summary>
    /// Resolves the canonical MIME type of a document by its extension.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <returns>The MIME type, or <see langword="null"/> when the extension is not allowed.</returns>
    public static string? ResolveDocumentType(string fileName)
        => DocumentTypes.TryGetValue(Path.GetExtension(fileName), out var type) ? type : null;
}
