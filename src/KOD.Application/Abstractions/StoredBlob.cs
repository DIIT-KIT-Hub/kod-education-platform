namespace KOD.Application.Abstractions;

/// <summary>
/// Result of storing a file.
/// </summary>
/// <param name="StorageKey">The key of the content in the storage.</param>
/// <param name="Size">The size in bytes.</param>
/// <param name="Sha256">The lowercase hexadecimal SHA-256 digest.</param>
public sealed record StoredBlob(string StorageKey, long Size, string Sha256);
