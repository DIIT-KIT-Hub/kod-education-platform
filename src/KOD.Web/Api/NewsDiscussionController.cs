using KOD.Application.Features.News.Discussion;
using KOD.Domain.Access;
using KOD.Domain.News;
using KOD.Web.Api.Contracts;
using KOD.Web.Security;
using Microsoft.AspNetCore.Mvc;

namespace KOD.Web.Api;

/// <summary>
/// Reactions and comments under news articles.
/// </summary>
/// <param name="discussion">The discussion service.</param>
[Route("api/v1/news/{articleId:guid}")]
[RequirePermission(Permissions.News.View)]
public sealed class NewsDiscussionController(INewsDiscussionService discussion) : ApiControllerBase
{
    /// <summary>
    /// Gets the reactions to an article and one page of its comment threads, each with its whole branch of replies.
    /// </summary>
    /// <param name="articleId">The article.</param>
    /// <param name="page">The 1-based page of threads.</param>
    /// <param name="pageSize">The number of threads per page (1–50).</param>
    /// <param name="sort">The order of the threads.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The discussion.</returns>
    [HttpGet("discussion")]
    public async Task<ActionResult<NewsDiscussion>> Get(
        Guid articleId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = INewsDiscussionService.PageSize,
        [FromQuery] CommentSort sort = CommentSort.Best,
        CancellationToken cancellationToken = default)
        => FromResult(await discussion.GetAsync(articleId, page, pageSize, sort, cancellationToken));

    /// <summary>
    /// Sets the current user's reaction to the article; the same reaction again removes it.
    /// </summary>
    /// <param name="articleId">The article.</param>
    /// <param name="kind">The emotion.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The reactions to the article.</returns>
    [HttpPost("reactions/{kind}")]
    public async Task<ActionResult<ReactionSummary>> React(Guid articleId, ReactionKind kind, CancellationToken cancellationToken)
        => FromResult(await discussion.ReactAsync(articleId, null, kind, cancellationToken));

    /// <summary>
    /// Sets the current user's reaction to a comment; the same reaction again removes it.
    /// </summary>
    /// <param name="articleId">The article.</param>
    /// <param name="commentId">The comment.</param>
    /// <param name="kind">The emotion.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The reactions to the comment.</returns>
    [HttpPost("comments/{commentId:guid}/reactions/{kind}")]
    public async Task<ActionResult<ReactionSummary>> ReactToComment(Guid articleId, Guid commentId, ReactionKind kind, CancellationToken cancellationToken)
        => FromResult(await discussion.ReactAsync(articleId, commentId, kind, cancellationToken));

    /// <summary>
    /// Adds a comment or a reply.
    /// </summary>
    /// <param name="articleId">The article.</param>
    /// <param name="request">The comment.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>201 with the new identifier.</returns>
    [HttpPost("comments")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult> Comment(Guid articleId, CommentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await discussion.CommentAsync(articleId, request.ParentId, request.Body, cancellationToken);
        return result.IsSuccess ? Created($"/api/v1/news/{articleId}/discussion", new { id = result.Value }) : ProblemFor(result.Error);
    }

    /// <summary>
    /// Replaces the text of the current user's comment.
    /// </summary>
    /// <param name="articleId">The article.</param>
    /// <param name="commentId">The comment.</param>
    /// <param name="request">The new text.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpPut("comments/{commentId:guid}")]
    public async Task<ActionResult> Edit(Guid articleId, Guid commentId, CommentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return FromResult(await discussion.EditCommentAsync(articleId, commentId, request.Body, cancellationToken));
    }

    /// <summary>
    /// Deletes a comment (the author's own, or anyone's for a news manager of the article).
    /// </summary>
    /// <param name="articleId">The article.</param>
    /// <param name="commentId">The comment.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>204 on success.</returns>
    [HttpDelete("comments/{commentId:guid}")]
    public async Task<ActionResult> Delete(Guid articleId, Guid commentId, CancellationToken cancellationToken)
        => FromResult(await discussion.DeleteCommentAsync(articleId, commentId, cancellationToken));
}
