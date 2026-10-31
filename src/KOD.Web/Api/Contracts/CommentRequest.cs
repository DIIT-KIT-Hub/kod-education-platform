using System.ComponentModel.DataAnnotations;
using KOD.Domain.News;

namespace KOD.Web.Api.Contracts;

/// <summary>
/// Body of <c>POST /api/v1/news/{articleId}/comments</c> and <c>PUT …/comments/{id}</c>.
/// </summary>
/// <param name="Body">The plain text.</param>
/// <param name="ParentId">The comment to reply to (ignored when editing).</param>
public sealed record CommentRequest(
    [Required][MaxLength(NewsComment.BodyMaxLength)] string Body,
    Guid? ParentId = null);
