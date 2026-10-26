using System.ComponentModel.DataAnnotations;
using KOD.Application.Features.News;
using KOD.Domain.News;

namespace KOD.Web.Api.Contracts;

/// <summary>
/// Body of <c>POST /api/v1/news</c> and <c>PUT /api/v1/news/{id}</c>.
/// </summary>
/// <param name="Title">The title.</param>
/// <param name="Summary">The lead.</param>
/// <param name="Body">The HTML body (sanitized by the server; images must be uploaded through <c>POST /api/v1/media</c>).</param>
/// <param name="Category">The category.</param>
/// <param name="IsPinned">Whether the article is pinned.</param>
/// <param name="Publish">Whether the article is published.</param>
/// <param name="RemoveCover">Whether to remove the cover image.</param>
/// <param name="OrganizationId">The organization of the article; omitted for news of the whole cabinet.</param>
/// <param name="AllowComments">Whether readers may comment (default: yes).</param>
public sealed record NewsRequest(
    [property: Required][property: MaxLength(NewsArticle.TitleMaxLength)] string Title,
    [property: Required][property: MaxLength(NewsArticle.SummaryMaxLength)] string Summary,
    [property: Required][property: MaxLength(NewsArticle.BodyMaxLength)] string Body,
    NewsCategory Category,
    bool IsPinned,
    bool Publish,
    bool RemoveCover,
    Guid? OrganizationId,
    bool AllowComments = true)
{
    /// <summary>
    /// Converts the request to an application command.
    /// </summary>
    /// <returns>The command.</returns>
    public SaveNewsCommand ToCommand() => new(Title, Summary, Body, Category, IsPinned, Publish, null, RemoveCover, OrganizationId, AllowComments);
}
