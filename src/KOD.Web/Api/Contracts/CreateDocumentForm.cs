using System.ComponentModel.DataAnnotations;
using KOD.Domain.Documents;

namespace KOD.Web.Api.Contracts;

/// <summary>
/// Multipart form of <c>POST /api/v1/documents</c>.
/// </summary>
public sealed class CreateDocumentForm
{
    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    [Required]
    [MaxLength(Document.TitleMaxLength)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    [MaxLength(Document.DescriptionMaxLength)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the document kind.
    /// </summary>
    public DocumentKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the unit the document belongs to.
    /// </summary>
    [Required]
    public Guid OrganizationUnitId { get; set; }

    /// <summary>
    /// Gets or sets the signing order.
    /// </summary>
    public SigningMode SigningMode { get; set; }

    /// <summary>
    /// Gets or sets the signers in signing order.
    /// </summary>
    public List<Guid> SignerIds { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether to send the document for signing immediately.
    /// </summary>
    public bool Submit { get; set; }

    /// <summary>
    /// Gets or sets the document file.
    /// </summary>
    [Required]
    public IFormFile? File { get; set; }
}
