using KOD.Application.Common.Results;
using KOD.Application.Features.Chats;
using KOD.Application.Features.Notifications;
using KOD.Domain.Notifications;

namespace KOD.UnitTests.Application;

/// <summary>
/// Notifications: replies in news, collected chat messages, reading and privacy.
/// </summary>
public sealed class NotificationTests : IDisposable
{
    /// <summary>
    /// The cabinet with organizations A (with subsidiary S) and B.
    /// </summary>
    private readonly TwoOrganizations _world = new();

    public void Dispose() => _world.Dispose();

    [Fact]
    public async Task A_reply_reaches_the_author_of_the_comment_but_not_its_own_writer()
    {
        var article = _world.News("Загальна новина", null);
        var root = (await _world.As(_world.EmployeeA).Discussion.CommentAsync(article.Id, null, "Питання", CancellationToken.None)).Ok();

        await _world.As(_world.EmployeeA).Discussion.CommentAsync(article.Id, root, "Доповнення", CancellationToken.None);
        Assert.Empty(_world.NotificationEvents.Received);

        var reply = (await _world.As(_world.EmployeeB).Discussion.CommentAsync(article.Id, root, "Відповідь", CancellationToken.None)).Ok();

        var told = Assert.Single(_world.NotificationEvents.Received);
        Assert.Equal(_world.EmployeeA.Id, told.UserId);
        Assert.Equal(NotificationKind.NewsReply, told.Notification.Kind);
        Assert.Equal(_world.EmployeeB.Id, told.Notification.Actor?.Id);
        Assert.EndsWith($"#comment-{reply}", told.Notification.Link, StringComparison.Ordinal);
        Assert.Equal(1, await _world.As(_world.EmployeeA).Notifications.CountUnreadAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Messages_of_a_chat_collect_in_one_notification_until_the_chat_is_read()
    {
        var a = _world.As(_world.EmployeeA).Chats;
        var chat = (await a.OpenDirectAsync(_world.EmployeeB.Id, CancellationToken.None)).Ok();
        await a.SendAsync(chat, Text("Перше"), CancellationToken.None);
        var second = (await a.SendAsync(chat, Text("Друге"), CancellationToken.None)).Ok();

        var b = _world.As(_world.EmployeeB);
        var collected = Assert.Single((await b.Notifications.ListAsync(new NotificationQuery(), CancellationToken.None)).Items);
        Assert.Equal(2, collected.Count);
        Assert.Equal("Друге", collected.Preview);
        Assert.DoesNotContain(_world.NotificationEvents.Received, r => r.UserId == _world.EmployeeA.Id);

        await b.Chats.MarkReadAsync(chat, second.Sequence, CancellationToken.None);
        Assert.Equal(0, await b.Notifications.CountUnreadAsync(CancellationToken.None));

        await a.SendAsync(chat, Text("Третє"), CancellationToken.None);
        Assert.Equal(2, (await b.Notifications.ListAsync(new NotificationQuery(), CancellationToken.None)).TotalCount);
        Assert.Equal(1, await b.Notifications.CountUnreadAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Notifications_are_read_and_deleted_only_by_their_recipient()
    {
        var a = _world.As(_world.EmployeeA).Chats;
        var chat = (await a.OpenDirectAsync(_world.EmployeeB.Id, CancellationToken.None)).Ok();
        await a.SendAsync(chat, Text("Привіт"), CancellationToken.None);
        var b = _world.As(_world.EmployeeB).Notifications;
        var id = Assert.Single((await b.ListAsync(new NotificationQuery { UnreadOnly = true }, CancellationToken.None)).Items).Id;

        Assert.Equal(ErrorType.NotFound, (await _world.As(_world.EmployeeA).Notifications.MarkReadAsync(id, CancellationToken.None)).Failure());
        Assert.Equal(ErrorType.NotFound, (await _world.As(_world.EmployeeA).Notifications.DeleteAsync(id, CancellationToken.None)).Failure());

        await b.MarkAllReadAsync(CancellationToken.None);
        Assert.Empty((await b.ListAsync(new NotificationQuery { UnreadOnly = true }, CancellationToken.None)).Items);
        Assert.Contains(_world.NotificationEvents.Changed, c => c.UserId == _world.EmployeeB.Id && c.UnreadCount == 0);

        await b.DeleteReadAsync(CancellationToken.None);
        Assert.Equal(0, (await b.ListAsync(new NotificationQuery(), CancellationToken.None)).TotalCount);
    }

    /// <summary>
    /// Builds a text message.
    /// </summary>
    /// <param name="body">The HTML.</param>
    /// <returns>The message.</returns>
    private static SendChatMessage Text(string body) => new(Guid.NewGuid(), body, []);
}
