using System.ComponentModel.DataAnnotations;
using KOD.Domain.Documents;

namespace KOD.Web.Api.Contracts;

/// <summary>
/// Multipart form of <c>POST /api/v1/documents/{id}/signatures</c>: a signature created in the browser by the IIT library.
/// </summary>
public sealed class SignatureUploadForm
{
    /// <summary>
    /// Gets or sets the signature format.
    /// </summary>
    public SignatureFormat Format { get; set; }

    /// <summary>
    /// Gets or sets the packaging.
    /// </summary>
    public SignaturePackaging Packaging { get; set; }

    /// <summary>
    /// Gets or sets the signature level (e.g. <c>B-LT</c>).
    /// </summary>
    [MaxLength(32)]
    public string? Level { get; set; }

    /// <summary>
    /// Gets or sets the key storage type.
    /// </summary>
    public KeyMediaType KeyMedia { get; set; }

    /// <summary>
    /// Gets or sets the signing time reported by the library (time-stamp or local time).
    /// </summary>
    public DateTimeOffset? SignedAt { get; set; }

    /// <summary>
    /// Gets or sets the signer's full name from the certificate.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string SubjectFullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the organization from the certificate.
    /// </summary>
    [MaxLength(256)]
    public string? SubjectOrganization { get; set; }

    /// <summary>
    /// Gets or sets the signer's taxpayer number (DRFO) or EDRPOU code.
    /// </summary>
    [MaxLength(32)]
    public string? SubjectTaxId { get; set; }

    /// <summary>
    /// Gets or sets the issuer (QTSP) name.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string IssuerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the certificate serial number.
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the start of the certificate validity.
    /// </summary>
    public DateTimeOffset? ValidFrom { get; set; }

    /// <summary>
    /// Gets or sets the end of the certificate validity.
    /// </summary>
    public DateTimeOffset? ValidTo { get; set; }

    /// <summary>
    /// Gets or sets the signature file.
    /// </summary>
    [Required]
    public IFormFile? Signature { get; set; }
}
