using System.ComponentModel.DataAnnotations;
using ODC.Web.Features.Documents.Domain;

namespace ODC.Web.Features.Documents;

/// <summary>
/// Body of <c>POST /api/v1/documents/{id}/rejections</c>.
/// </summary>
/// <param name="Reason">The reason of the rejection.</param>
public sealed record RejectionRequest([Required][MaxLength(DocumentSigner.CommentMaxLength)] string Reason);
