using System.ComponentModel.DataAnnotations;
using ODC.Web.Features.Documents.Domain;

namespace ODC.Web.Features.Documents;

/// <summary>
/// Multipart form that changes a draft or rejected document. Without <see cref="File"/> the current file is kept.
/// </summary>
public sealed class UpdateDocumentForm
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
    /// Gets or sets the kind.
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
    /// Gets or sets a value indicating whether to send the document for signing after saving.
    /// </summary>
    public bool Submit { get; set; }
}
