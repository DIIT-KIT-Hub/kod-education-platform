using KOD.Domain.Common;

namespace KOD.Domain.Documents;

/// <summary>
/// Metadata of a binary file kept in the private file storage.
/// </summary>
public sealed record StoredFile
{
    /// <summary>
    /// Maximum length of <see cref="FileName"/>.
    /// </summary>
    public const int FileNameMaxLength = 255;

    /// <summary>
    /// Initializes a new instance of the <see cref="StoredFile"/> class.
    /// </summary>
    /// <param name="fileName">The original file name shown to users.</param>
    /// <param name="contentType">The MIME type.</param>
    /// <param name="size">The size in bytes.</param>
    /// <param name="sha256">The lowercase hexadecimal SHA-256 digest of the content.</param>
    /// <param name="storageKey">The key of the content in the file storage.</param>
    public StoredFile(string fileName, string contentType, long size, string sha256, string storageKey)
    {
        FileName = Guard.Text(Path.GetFileName(fileName), FileNameMaxLength, "Назва файлу");
        ContentType = Guard.Text(contentType, 128, "Тип файлу");
        Size = size > 0 ? size : throw new DomainException("Файл порожній.");
        Sha256 = sha256.Length == 64 ? sha256.ToLowerInvariant() : throw new DomainException("Некоректний хеш файлу.");
        StorageKey = Guard.Text(storageKey, 512, "Ключ сховища");
    }

    /// <summary>
    /// Gets the original file name.
    /// </summary>
    public string FileName { get; private init; }

    /// <summary>
    /// Gets the MIME type.
    /// </summary>
    public string ContentType { get; private init; }

    /// <summary>
    /// Gets the size in bytes.
    /// </summary>
    public long Size { get; private init; }

    /// <summary>
    /// Gets the lowercase hexadecimal SHA-256 digest of the content.
    /// </summary>
    public string Sha256 { get; private init; }

    /// <summary>
    /// Gets the key of the content in the file storage.
    /// </summary>
    public string StorageKey { get; private init; }

    /// <summary>
    /// Gets a value indicating whether the file is a PDF document (eligible for PAdES).
    /// </summary>
    public bool IsPdf => ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
        || FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
}
