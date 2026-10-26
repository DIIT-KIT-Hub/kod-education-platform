using KOD.Application.Common.Files;
using KOD.Application.Common.Paging;
using KOD.Application.Features.News;
using KOD.Domain.Access;
using KOD.Web.Api.Contracts;
using KOD.Web.Security;
using Microsoft.AspNetCore.Mvc;

namespace KOD.Web.Api;

/// <summary>
/// News feed and news management.
/// </summary>
/// <param name="news">The news service.</param>
[Route("api/v1/news")]
[RequirePermission(Permissions.News.View)]
public sealed class NewsController(INewsService news) : ApiControllerBase
{
    /// <summary>
    /// Lists published news (managers may include drafts).
    /// </summary>
    /// <param name="query">Paging and filters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A page of news.</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResult<NewsListItem>>> List([FromQuery] NewsQuery query, CancellationToken cancellationToken)
        => FromResult(await news.ListAsync(query, cancellationToken));

    /// <summary>
    /// Gets an article by its slug.
    /// </summary>
    /// <param name="slug">The slug.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The article.</returns>
    [HttpGet("{slug}")]
    public async Task<ActionResult<NewsArticleDto>> Get(string slug, CancellationToken cancellationToken)
        => FromResult(await news.GetBySlugAsync(slug, cancellationToken));

    /// <summary>
    /// Creates an article.
    /// </summary>
    /// <param name="request">The article.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>201 with the new identifier.</returns>
    [HttpPost]
    [RequirePermission(Permissions.News.Manage)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult> Create(NewsRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await news.CreateAsync(request.ToCommand(), cancellationToken);
        return result.IsSuccess ? Created($"/api/v1/news/{result.Value}", new { id = result.Value }) : ProblemFor(result.Error);
    }

    /// <summary>
    /// Replaces an article.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="request">The article.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.News.Manage)]
    public async Task<ActionResult> Update(Guid id, NewsRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return FromResult(await news.UpdateAsync(id, request.ToCommand(), cancellationToken));
    }

    /// <summary>
    /// Replaces the cover image (JPEG, PNG, WebP, AVIF or GIF up to 15 MB, stored as AVIF).
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="file">The image.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpPut("{id:guid}/cover")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FileRules.MaxImageSize + (64 * 1024))]
    [RequirePermission(Permissions.News.Manage)]
    public async Task<ActionResult> SetCover(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);

        var current = await news.GetAsync(id, cancellationToken);
        if (!current.IsSuccess)
        {
            return ProblemFor(current.Error);
        }

        await using var content = file.OpenReadStream();
        var article = current.Value;
        var command = new SaveNewsCommand(
            article.Title,
            article.Summary,
            article.BodyHtml,
            article.Category,
            article.IsPinned,
            article.PublishedAt is not null,
            new FileUpload(content, file.FileName, file.ContentType, file.Length),
            RemoveCover: false,
            article.Organization?.Id,
            article.AllowComments);
        return FromResult(await news.UpdateAsync(id, command, cancellationToken));
    }

    /// <summary>
    /// Deletes an article.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.News.Manage)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => FromResult(await news.DeleteAsync(id, cancellationToken));
}
