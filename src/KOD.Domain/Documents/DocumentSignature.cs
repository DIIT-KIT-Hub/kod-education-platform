using KOD.Domain.Common;
using KOD.Domain.Users;

namespace KOD.Domain.Documents;

/// <summary>
/// A qualified electronic signature created in the signer's browser and attached to a document.
/// The server never sees the private key or its password; it only stores the resulting signature.
/// </summary>
public sealed class DocumentSignature : Entity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentSignature"/> class.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="userId">The signer's user identifier.</param>
    /// <param name="format">The signature format.</param>
    /// <param name="packaging">Whether the data is embedded in the signature.</param>
    /// <param name="level">The signature level reported by the library (e.g. <c>B-LT</c>).</param>
    /// <param name="keyMedia">The type of the key storage.</param>
    /// <param name="file">The stored signature file.</param>
    /// <param name="certificate">The signer's certificate data.</param>
    /// <param name="signedAt">The signing time (from the time-stamp when available).</param>
    /// <param name="createdAt">The moment the signature was received.</param>
    /// <param name="baseSignatureId">The signature whose file this one was added to (cumulative formats).</param>
    internal DocumentSignature(
        Guid documentId,
        Guid userId,
        SignatureFormat format,
        SignaturePackaging packaging,
        string? level,
        KeyMediaType keyMedia,
        StoredFile file,
        SignerCertificate certificate,
        DateTimeOffset signedAt,
        DateTimeOffset createdAt,
        Guid? baseSignatureId)
    {
        DocumentId = documentId;
        UserId = userId;
        Format = format;
        Packaging = packaging;
        Level = Guard.OptionalText(level, 32, "Рівень підпису");
        KeyMedia = keyMedia;
        File = file;
        Certificate = certificate;
        SignedAt = signedAt;
        CreatedAt = createdAt;
        BaseSignatureId = baseSignatureId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentSignature"/> class for EF Core materialization.
    /// </summary>
    private DocumentSignature()
    {
        File = null!;
        Certificate = null!;
    }

    /// <summary>
    /// Gets the signature whose file this one was added to, or <see langword="null"/> for the first signature and for
    /// independent (XAdES) signatures. The file of a cumulative signature holds this signature and all earlier ones.
    /// </summary>
    public Guid? BaseSignatureId { get; private set; }

    /// <summary>
    /// Gets the document identifier.
    /// </summary>
    public Guid DocumentId { get; private set; }

    /// <summary>
    /// Gets the signer's user identifier.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the signer.
    /// </summary>
    public User? User { get; private set; }

    /// <summary>
    /// Gets the signature format.
    /// </summary>
    public SignatureFormat Format { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the data is embedded in the signature.
    /// </summary>
    public SignaturePackaging Packaging { get; private set; }

    /// <summary>
    /// Gets the signature level (e.g. <c>B-B</c>, <c>B-T</c>, <c>B-LT</c>, <c>B-LTA</c>).
    /// </summary>
    public string? Level { get; private set; }

    /// <summary>
    /// Gets the type of the key storage.
    /// </summary>
    public KeyMediaType KeyMedia { get; private set; }

    /// <summary>
    /// Gets the stored signature file.
    /// </summary>
    public StoredFile File { get; private set; }

    /// <summary>
    /// Gets the signer's certificate data.
    /// </summary>
    public SignerCertificate Certificate { get; private set; }

    /// <summary>
    /// Gets the signing time.
    /// </summary>
    public DateTimeOffset SignedAt { get; private set; }

    /// <summary>
    /// Gets the moment the signature was received by the server.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }
}
