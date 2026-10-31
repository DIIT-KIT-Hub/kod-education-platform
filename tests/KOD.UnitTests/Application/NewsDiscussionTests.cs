using KOD.Application.Common.Files;
using KOD.Application.Common.Media;
using KOD.Application.Common.Results;
using KOD.Application.Features.News;
using KOD.Application.Features.News.Discussion;
using KOD.Domain.News;

namespace KOD.UnitTests.Application;

public sealed class NewsDiscussionTests : IDisposable
{
    /// <summary>
    /// The cabinet with organizations A (with subsidiary S) and B.
    /// </summary>
    private readonly TwoOrganizations _world = new();

    public void Dispose() => _world.Dispose();

    [Fact]
    public async Task A_reader_has_one_reaction_that_changes_and_is_removed_by_choosing_it_again()
    {
        var article = _world.News("Свято", _world.A);
        var reader = _world.As(_world.EmployeeB).Discussion;

        Assert.Equal(ReactionKind.Like, (await reader.ReactAsync(article.Id, null, ReactionKind.Like, CancellationToken.None)).Ok().Mine);
        var changed = (await reader.ReactAsync(article.Id, null, ReactionKind.Love, CancellationToken.None)).Ok();
        await _world.As(_world.EmployeeA).Discussion.ReactAsync(article.Id, null, ReactionKind.Love, CancellationToken.None);
        var removed = (await reader.ReactAsync(article.Id, null, ReactionKind.Love, CancellationToken.None)).Ok();

        Assert.Equal(ReactionKind.Love, changed.Mine);
        Assert.Equal(1, changed.Total);
        Assert.Null(removed.Mine);
        Assert.Equal(1, removed.Total);
    }

    [Fact]
    public async Task A_reply_goes_under_the_very_comment_it_answers_and_names_its_author()
    {
        var article = _world.News("Новина", _world.A);
        var author = _world.As(_world.EmployeeA).Discussion;
        var other = _world.As(_world.EmployeeB).Discussion;

        var root = (await author.CommentAsync(article.Id, null, "Перший", CancellationToken.None)).Ok();
        var reply = (await other.CommentAsync(article.Id, root, "Відповідь", CancellationToken.None)).Ok();
        var deeper = (await author.CommentAsync(article.Id, reply, "Відповідь на відповідь", CancellationToken.None)).Ok();

        var thread = Assert.Single((await other.GetAsync(article.Id, 1, 10, CommentSort.Newest, CancellationToken.None)).Ok().Comments);
        Assert.Equal(2, thread.ReplyCount);
        var first = Assert.Single(thread.Replies);
        Assert.Equal(reply, first.Id);
        Assert.Equal(1, first.Depth);
        Assert.Equal(_world.EmployeeA.Id, first.ReplyTo?.Id);
        var second = Assert.Single(first.Replies);
        Assert.Equal(deeper, second.Id);
        Assert.Equal(2, second.Depth);
        Assert.Equal(_world.EmployeeB.Id, second.ReplyTo?.Id);
    }

    [Fact]
    public async Task A_deleted_comment_keeps_its_branch_and_goes_away_with_the_last_reply()
    {
        var article = _world.News("Новина", _world.A);
        var author = _world.As(_world.EmployeeA).Discussion;
        var other = _world.As(_world.EmployeeB).Discussion;

        var root = (await author.CommentAsync(article.Id, null, "Перший", CancellationToken.None)).Ok();
        var reply = (await other.CommentAsync(article.Id, root, "Відповідь", CancellationToken.None)).Ok();
        Assert.True((await author.DeleteCommentAsync(article.Id, root, CancellationToken.None)).IsSuccess);

        var discussion = (await other.GetAsync(article.Id, 1, 10, CommentSort.Best, CancellationToken.None)).Ok();
        var thread = Assert.Single(discussion.Comments);
        Assert.True(thread.IsDeleted);
        Assert.Equal(string.Empty, thread.Body);
        Assert.Equal(1, discussion.CommentCount);

        // Nobody can answer a deleted comment; when its last reply goes, the placeholder goes too.
        Assert.Equal(ErrorType.Validation, (await other.CommentAsync(article.Id, root, "Ще", CancellationToken.None)).Failure());
        Assert.True((await other.DeleteCommentAsync(article.Id, reply, CancellationToken.None)).IsSuccess);
        Assert.Empty(_world.Db.NewsComments);
        Assert.Empty((await other.GetAsync(article.Id, 1, 10, CommentSort.Best, CancellationToken.None)).Ok().Comments);
    }

    [Fact]
    public async Task Threads_are_paged_with_their_whole_branches()
    {
        var article = _world.News("Новина", _world.A);
        var reader = _world.As(_world.EmployeeB).Discussion;
        for (var i = 1; i <= 3; i++)
        {
            var thread = (await reader.CommentAsync(article.Id, null, $"Гілка {i}", CancellationToken.None)).Ok();
            (await reader.CommentAsync(article.Id, thread, $"Відповідь {i}", CancellationToken.None)).Ok();
        }

        var second = (await reader.GetAsync(article.Id, 2, 2, CommentSort.Oldest, CancellationToken.None)).Ok();
        Assert.Equal(3, second.ThreadCount);
        Assert.Equal(6, second.CommentCount);
        var last = Assert.Single(second.Comments);
        Assert.Equal("Гілка 3", last.Body);
        Assert.Equal("Відповідь 3", Assert.Single(last.Replies).Body);
    }

    [Fact]
    public async Task Only_the_author_edits_and_only_news_managers_of_the_organization_delete_other_comments()
    {
        var article = _world.News("Новина", _world.A);
        var comment = (await _world.As(_world.EmployeeB).Discussion.CommentAsync(article.Id, null, "Текст", CancellationToken.None)).Ok();

        Assert.Equal(ErrorType.Forbidden, (await _world.As(_world.EmployeeA).Discussion.EditCommentAsync(article.Id, comment, "Чуже", CancellationToken.None)).Failure());
        Assert.Equal(ErrorType.Forbidden, (await _world.As(_world.EmployeeA).Discussion.DeleteCommentAsync(article.Id, comment, CancellationToken.None)).Failure());

        // The comment is addressed through its own article only.
        var otherArticle = _world.News("Інша новина", _world.A);
        Assert.Equal(ErrorType.NotFound, (await _world.As(_world.StewardA).Discussion.DeleteCommentAsync(otherArticle.Id, comment, CancellationToken.None)).Failure());
        Assert.True((await _world.As(_world.StewardA).Discussion.DeleteCommentAsync(article.Id, comment, CancellationToken.None)).IsSuccess);
    }

    [Fact]
    public async Task Comments_can_be_switched_off_and_drafts_have_no_discussion()
    {
        var closed = _world.News("Без коментарів", _world.A);
        closed.Update(closed.Title, closed.Summary, closed.Body, closed.Category, isPinned: false, allowComments: false);
        var draft = _world.News("Чернетка", _world.A);
        draft.Unpublish();
        await _world.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var reader = _world.As(_world.EmployeeB).Discussion;

        Assert.Equal(ErrorType.Validation, (await reader.CommentAsync(closed.Id, null, "Текст", CancellationToken.None)).Failure());
        Assert.True((await reader.ReactAsync(closed.Id, null, ReactionKind.Wow, CancellationToken.None)).IsSuccess);
        Assert.Equal(ErrorType.NotFound, (await reader.GetAsync(draft.Id, 1, 10, CommentSort.Best, CancellationToken.None)).Failure());
    }

    [Fact]
    public async Task Article_images_are_attached_on_save_and_deleted_when_the_text_drops_them()
    {
        var services = _world.As(_world.StewardA);
        var first = (await services.Media.UploadNewsImageAsync(Upload(), CancellationToken.None)).Ok();
        var second = (await services.Media.UploadNewsImageAsync(Upload(), CancellationToken.None)).Ok();
        var gallery = $"<div data-type=\"gallery\"><img src=\"{second.ThumbnailUrl}\" data-full=\"{second.Url}\"></div>";
        var command = new SaveNewsCommand("Фото", "Анонс", $"<p>Текст</p><img src=\"{first.Url}\">{gallery}", NewsCategory.News, false, true, null, false, _world.A.Id);

        var id = (await services.News.CreateAsync(command, CancellationToken.None)).Ok();
        Assert.All(_world.Db.MediaFiles, m => Assert.Equal(id, m.NewsArticleId));

        Assert.True((await services.News.UpdateAsync(id, command with { Body = gallery }, CancellationToken.None)).IsSuccess);
        Assert.DoesNotContain(first.Id, _world.Storage.Stored);
        Assert.Contains(second.Id, _world.Storage.Stored);
        Assert.Equal(second.Id, Assert.Single(_world.Db.MediaFiles).Id);
        Assert.Contains(MediaUrls.Of(second.Id), (await services.News.GetAsync(id, CancellationToken.None)).Ok().BodyHtml, StringComparison.Ordinal);
    }

    /// <summary>
    /// Creates an upload of a few bytes (the fake processor accepts anything).
    /// </summary>
    /// <returns>The upload.</returns>
    private static FileUpload Upload() => new(new MemoryStream([1, 2, 3, 4]), "photo.jpg", "image/jpeg", 4);
}
