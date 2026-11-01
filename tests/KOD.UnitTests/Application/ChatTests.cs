using KOD.Application.Common.Files;
using KOD.Application.Common.Results;
using KOD.Application.Features.Chats;
using KOD.Domain.Chats;

namespace KOD.UnitTests.Application;

/// <summary>
/// Chats: one-to-one and group membership, numbering, resends, read positions, sanitizing and attachments.
/// </summary>
public sealed class ChatTests : IDisposable
{
    /// <summary>
    /// The cabinet with organizations A (with subsidiary S) and B.
    /// </summary>
    private readonly TwoOrganizations _world = new();

    public void Dispose() => _world.Dispose();

    [Fact]
    public async Task There_is_one_direct_chat_per_pair_whoever_opens_it()
    {
        var first = (await _world.As(_world.EmployeeA).Chats.OpenDirectAsync(_world.EmployeeB.Id, CancellationToken.None)).Ok();
        var again = (await _world.As(_world.EmployeeA).Chats.OpenDirectAsync(_world.EmployeeB.Id, CancellationToken.None)).Ok();
        var other = (await _world.As(_world.EmployeeB).Chats.OpenDirectAsync(_world.EmployeeA.Id, CancellationToken.None)).Ok();

        Assert.Equal(first, again);
        Assert.Equal(first, other);
        Assert.Single(_world.Db.Chats);
        Assert.Equal(ErrorType.Validation, (await _world.As(_world.EmployeeA).Chats.OpenDirectAsync(_world.EmployeeA.Id, CancellationToken.None)).Failure());
    }

    [Fact]
    public async Task A_chat_does_not_exist_for_people_outside_it()
    {
        var chat = (await _world.As(_world.EmployeeA).Chats.OpenDirectAsync(_world.EmployeeB.Id, CancellationToken.None)).Ok();
        var outsider = _world.As(_world.EmployeeS).Chats;

        Assert.Equal(ErrorType.NotFound, (await outsider.GetAsync(chat, CancellationToken.None)).Failure());
        Assert.Equal(ErrorType.NotFound, (await outsider.GetMessagesAsync(chat, null, null, 50, CancellationToken.None)).Failure());
        Assert.Equal(ErrorType.NotFound, (await outsider.SendAsync(chat, Text("Привіт"), CancellationToken.None)).Failure());
        Assert.Null(await outsider.GetMemberIdsAsync(chat, CancellationToken.None));
        Assert.Empty(await outsider.ListAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Messages_are_numbered_in_order_and_a_resend_lands_once()
    {
        var a = _world.As(_world.EmployeeA).Chats;
        var chat = (await a.OpenDirectAsync(_world.EmployeeB.Id, CancellationToken.None)).Ok();
        var first = Text("Перше");

        var sent = (await a.SendAsync(chat, first, CancellationToken.None)).Ok();
        var second = (await a.SendAsync(chat, Text("Друге"), CancellationToken.None)).Ok();
        var resent = (await a.SendAsync(chat, first, CancellationToken.None)).Ok();

        Assert.Equal(1, sent.Sequence);
        Assert.Equal(2, second.Sequence);
        Assert.Equal(sent.Id, resent.Id);
        Assert.Equal(2, _world.Db.ChatMessages.Count());

        // Both members' pages get each new message once.
        Assert.Equal(2, _world.ChatEvents.Added.Count);
        Assert.All(_world.ChatEvents.Added, static e => Assert.Equal(2, e.Recipients.Count));
    }

    [Fact]
    public async Task Unread_messages_count_from_the_read_position_which_only_moves_forward()
    {
        var a = _world.As(_world.EmployeeA).Chats;
        var b = _world.As(_world.EmployeeB).Chats;
        var chat = (await a.OpenDirectAsync(_world.EmployeeB.Id, CancellationToken.None)).Ok();
        await a.SendAsync(chat, Text("Раз"), CancellationToken.None);
        await a.SendAsync(chat, Text("Два"), CancellationToken.None);

        Assert.Equal(2, Assert.Single(await b.ListAsync(CancellationToken.None)).UnreadCount);
        Assert.Equal(0, Assert.Single(await a.ListAsync(CancellationToken.None)).UnreadCount);

        Assert.True((await b.MarkReadAsync(chat, 1, CancellationToken.None)).IsSuccess);
        Assert.True((await b.MarkReadAsync(chat, 0, CancellationToken.None)).IsSuccess);
        var summary = Assert.Single(await b.ListAsync(CancellationToken.None));
        Assert.Equal(1, summary.UnreadCount);
        Assert.Equal(1, summary.LastReadSequence);

        // A position beyond the newest message stops at it.
        await b.MarkReadAsync(chat, 99, CancellationToken.None);
        Assert.Equal(2, Assert.Single(await b.ListAsync(CancellationToken.None)).LastReadSequence);
    }

    [Fact]
    public async Task Only_text_formatting_survives_in_a_message()
    {
        var a = _world.As(_world.EmployeeA).Chats;
        var chat = (await a.OpenDirectAsync(_world.EmployeeB.Id, CancellationToken.None)).Ok();

        var message = (await a.SendAsync(
            chat,
            Text("<p><strong>Жирний</strong> <a href=\"https://example.com\" onclick=\"x()\">лінк</a><script>alert(1)</script><img src=\"/media/x\"></p><ul><li>пункт</li></ul>"),
            CancellationToken.None)).Ok();

        Assert.Contains("<strong>Жирний</strong>", message.Body, StringComparison.Ordinal);
        Assert.Contains("rel=\"noopener noreferrer nofollow\"", message.Body, StringComparison.Ordinal);
        Assert.Contains("<li>пункт</li>", message.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("script", message.Body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", message.Body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<img", message.Body, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Жирний лінк\nпункт", message.Text);

        Assert.Equal(ErrorType.Validation, (await a.SendAsync(chat, Text("<p> </p>"), CancellationToken.None)).Failure());
    }

    [Fact]
    public async Task Anyone_leaves_a_group_but_only_its_owner_removes_others()
    {
        var owner = _world.As(_world.EmployeeA).Chats;
        var chat = (await owner.CreateGroupAsync("Проєкт", [_world.EmployeeB.Id, _world.EmployeeS.Id], CancellationToken.None)).Ok();

        Assert.Equal(ErrorType.Validation, (await _world.As(_world.EmployeeB).Chats.RemoveMemberAsync(chat, _world.EmployeeS.Id, CancellationToken.None)).Failure());
        Assert.True((await _world.As(_world.EmployeeS).Chats.RemoveMemberAsync(chat, _world.EmployeeS.Id, CancellationToken.None)).IsSuccess);
        Assert.True((await owner.RemoveMemberAsync(chat, _world.EmployeeB.Id, CancellationToken.None)).IsSuccess);

        var details = (await owner.GetAsync(chat, CancellationToken.None)).Ok();
        Assert.Equal(_world.EmployeeA.Id, Assert.Single(details.Members).User.Id);
        Assert.Contains(_world.ChatEvents.Removed, e => e.Recipients.Contains(_world.EmployeeS.Id));
        Assert.Equal(ErrorType.NotFound, (await _world.As(_world.EmployeeB).Chats.GetAsync(chat, CancellationToken.None)).Failure());

        // Created, left, removed: the chat tells its story.
        var notes = (await owner.GetMessagesAsync(chat, null, null, 50, CancellationToken.None)).Ok().Messages;
        Assert.Equal([ChatEvent.Created, ChatEvent.MemberLeft, ChatEvent.MemberRemoved], notes.Select(static m => m.Event!.Value));

        // The last member leaving deletes the chat.
        Assert.True((await owner.RemoveMemberAsync(chat, _world.EmployeeA.Id, CancellationToken.None)).IsSuccess);
        Assert.Empty(_world.Db.Chats);
    }

    [Fact]
    public async Task People_without_chats_cannot_be_found_or_added()
    {
        var a = _world.As(_world.EmployeeA).Chats;

        var found = await a.SearchPeopleAsync("steward", CancellationToken.None);
        Assert.Empty(found);
        Assert.Contains(await a.SearchPeopleAsync("employee", CancellationToken.None), p => p.Id == _world.EmployeeB.Id);
        Assert.DoesNotContain(await a.SearchPeopleAsync("employee", CancellationToken.None), p => p.Id == _world.EmployeeA.Id);
        Assert.Equal(ErrorType.Validation, (await a.OpenDirectAsync(_world.StewardA.Id, CancellationToken.None)).Failure());
        Assert.Equal(ErrorType.Validation, (await a.CreateGroupAsync(null, [_world.EmployeeB.Id, _world.StewardA.Id], CancellationToken.None)).Failure());

        // A person without the permission does not use chats at all.
        Assert.Equal(ErrorType.Forbidden, (await _world.As(_world.StewardA).Chats.OpenDirectAsync(_world.EmployeeA.Id, CancellationToken.None)).Failure());
    }

    [Fact]
    public async Task Photos_and_allowed_files_are_attached_and_served_only_to_members()
    {
        var a = _world.As(_world.EmployeeA).Chats;
        var chat = (await a.OpenDirectAsync(_world.EmployeeB.Id, CancellationToken.None)).Ok();

        var message = (await a.SendAsync(chat, Text(string.Empty, File("фото.png"), File("звіт.pdf")), CancellationToken.None)).Ok();
        Assert.Equal([ChatAttachmentKind.Image, ChatAttachmentKind.File], message.Attachments.Select(static f => f.Kind));
        Assert.NotNull(message.Attachments[0].ThumbnailUrl);
        Assert.Null(message.Attachments[1].ThumbnailUrl);

        var photo = message.Attachments[0].Id;
        Assert.True((await _world.As(_world.EmployeeB).Chats.OpenAttachmentAsync(chat, photo, thumbnail: true, CancellationToken.None)).IsSuccess);
        Assert.Equal(ErrorType.NotFound, (await _world.As(_world.EmployeeS).Chats.OpenAttachmentAsync(chat, photo, thumbnail: false, CancellationToken.None)).Failure());

        // Executables and pages are refused; nothing of a refused message stays in the storage.
        var stored = _world.Files.Stored.Count;
        Assert.Equal(ErrorType.Validation, (await a.SendAsync(chat, Text("Ось", File("ok.pdf"), File("setup.exe")), CancellationToken.None)).Failure());
        Assert.Equal(ErrorType.Validation, (await a.SendAsync(chat, Text("Ось", File("page.html")), CancellationToken.None)).Failure());
        Assert.Equal(stored, _world.Files.Stored.Count);

        // Deleting the message deletes its files.
        Assert.True((await a.DeleteAsync(chat, message.Id, CancellationToken.None)).IsSuccess);
        Assert.Empty(_world.Files.Stored);
        Assert.Equal(ErrorType.Forbidden, (await _world.As(_world.EmployeeB).Chats.DeleteAsync(chat, message.Id, CancellationToken.None)).Failure());
    }

    [Fact]
    public async Task Forwarded_messages_name_the_original_author_and_own_copies_of_their_files()
    {
        var a = _world.As(_world.EmployeeA).Chats;
        var b = _world.As(_world.EmployeeB).Chats;
        var s = _world.As(_world.EmployeeS).Chats;
        var direct = (await a.OpenDirectAsync(_world.EmployeeB.Id, CancellationToken.None)).Ok();
        var original = (await a.SendAsync(direct, Text("<p>Звіт</p>", File("фото.png")), CancellationToken.None)).Ok();
        var group = (await b.CreateGroupAsync("Проєкт", [_world.EmployeeS.Id], CancellationToken.None)).Ok();

        var forwarded = Assert.Single((await b.ForwardAsync(group, direct, [original.Id], CancellationToken.None)).Ok());
        Assert.Equal(_world.EmployeeB.Id, forwarded.Author.Id);
        Assert.Equal(_world.EmployeeA.Id, forwarded.ForwardedFrom?.Id);
        Assert.Equal("Звіт", forwarded.Text);
        var copy = Assert.Single(forwarded.Attachments);
        Assert.NotNull(copy.ThumbnailUrl);

        // The group gets its own copy: it stays when the original is deleted.
        Assert.True((await a.DeleteAsync(direct, original.Id, CancellationToken.None)).IsSuccess);
        Assert.True((await s.OpenAttachmentAsync(group, copy.Id, thumbnail: true, CancellationToken.None)).IsSuccess);
        Assert.Equal(2, _world.Files.Stored.Count);

        // Forwarded again, it still names the first author; a forwarded message cannot be edited.
        var again = Assert.Single((await s.ForwardAsync(group, group, [forwarded.Id], CancellationToken.None)).Ok());
        Assert.Equal(_world.EmployeeA.Id, again.ForwardedFrom?.Id);
        Assert.Equal(ErrorType.Validation, (await s.EditAsync(group, again.Id, "<p>Інше</p>", CancellationToken.None)).Failure());

        // Only from the user's own chats, and only messages they still see.
        Assert.Equal(ErrorType.NotFound, (await s.ForwardAsync(group, direct, [original.Id], CancellationToken.None)).Failure());
        Assert.Equal(ErrorType.Validation, (await b.ForwardAsync(group, direct, [original.Id], CancellationToken.None)).Failure());
        var note = (await b.GetMessagesAsync(group, null, null, 50, CancellationToken.None)).Ok().Messages[0];
        Assert.Equal(ErrorType.Validation, (await b.ForwardAsync(direct, group, [note.Id], CancellationToken.None)).Failure());
    }

    [Fact]
    public async Task Messages_are_paged_backwards_and_caught_up_forwards()
    {
        var a = _world.As(_world.EmployeeA).Chats;
        var chat = (await a.OpenDirectAsync(_world.EmployeeB.Id, CancellationToken.None)).Ok();
        for (var i = 1; i <= 5; i++)
        {
            await a.SendAsync(chat, Text($"Повідомлення {i}"), CancellationToken.None);
        }

        var newest = (await a.GetMessagesAsync(chat, null, null, 2, CancellationToken.None)).Ok();
        var older = (await a.GetMessagesAsync(chat, 4, null, 2, CancellationToken.None)).Ok();
        var missed = (await a.GetMessagesAsync(chat, null, 3, 10, CancellationToken.None)).Ok();

        Assert.Equal([4L, 5L], newest.Messages.Select(static m => m.Sequence));
        Assert.True(newest.HasMore);
        Assert.Equal([2L, 3L], older.Messages.Select(static m => m.Sequence));
        Assert.Equal([4L, 5L], missed.Messages.Select(static m => m.Sequence));
        Assert.False(missed.HasMore);
        Assert.Equal(5, missed.LastSequence);
    }

    [Fact]
    public async Task A_direct_chat_deleted_for_me_leaves_only_my_list_until_a_new_message()
    {
        var a = _world.As(_world.EmployeeA).Chats;
        var b = _world.As(_world.EmployeeB).Chats;
        var chat = (await a.OpenDirectAsync(_world.EmployeeB.Id, CancellationToken.None)).Ok();
        await a.SendAsync(chat, Text("Раз"), CancellationToken.None);
        await a.SendAsync(chat, Text("Два"), CancellationToken.None);

        Assert.True((await b.ClearHistoryAsync(chat, CancellationToken.None)).IsSuccess);

        Assert.Empty(await b.ListAsync(CancellationToken.None));
        Assert.Empty((await b.GetMessagesAsync(chat, null, null, 50, CancellationToken.None)).Ok().Messages);
        Assert.Equal(2, (await a.GetMessagesAsync(chat, null, null, 50, CancellationToken.None)).Ok().Messages.Count);
        Assert.Contains(_world.ChatEvents.Cleared, e => e.ChatId == chat && e.Recipients.SequenceEqual([_world.EmployeeB.Id]));

        // A new message brings the chat back with only what came after the clearing.
        await a.SendAsync(chat, Text("Три"), CancellationToken.None);
        var listed = Assert.Single(await b.ListAsync(CancellationToken.None));
        Assert.Equal("Три", listed.LastMessage?.Text);
        Assert.Equal(1, listed.UnreadCount);
        Assert.Equal([3L], (await b.GetMessagesAsync(chat, null, null, 50, CancellationToken.None)).Ok().Messages.Select(static m => m.Sequence));
    }

    [Fact]
    public async Task Either_person_deletes_a_direct_chat_for_both_but_only_the_owner_deletes_a_group()
    {
        var a = _world.As(_world.EmployeeA).Chats;
        var b = _world.As(_world.EmployeeB).Chats;
        var direct = (await a.OpenDirectAsync(_world.EmployeeB.Id, CancellationToken.None)).Ok();
        await a.SendAsync(direct, Text("Привіт"), CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, (await _world.As(_world.EmployeeS).Chats.DeleteChatAsync(direct, CancellationToken.None)).Failure());
        Assert.True((await b.DeleteChatAsync(direct, CancellationToken.None)).IsSuccess);
        Assert.Empty(_world.Db.Chats);
        var removed = Assert.Single(_world.ChatEvents.Removed);
        Assert.Equal([_world.EmployeeA.Id, _world.EmployeeB.Id], removed.Recipients.Order());

        var group = (await a.CreateGroupAsync("Проєкт", [_world.EmployeeB.Id], CancellationToken.None)).Ok();
        Assert.Equal(ErrorType.Forbidden, (await b.DeleteChatAsync(group, CancellationToken.None)).Failure());
        Assert.True((await a.DeleteChatAsync(group, CancellationToken.None)).IsSuccess);
        Assert.Empty(_world.Db.Chats);
    }

    [Fact]
    public async Task A_message_deleted_for_me_becomes_an_empty_placeholder_only_for_me()
    {
        var a = _world.As(_world.EmployeeA).Chats;
        var b = _world.As(_world.EmployeeB).Chats;
        var chat = (await a.OpenDirectAsync(_world.EmployeeB.Id, CancellationToken.None)).Ok();
        var secret = (await a.SendAsync(chat, Text("Секрет"), CancellationToken.None)).Ok();
        var last = (await a.SendAsync(chat, Text("Останнє"), CancellationToken.None)).Ok();

        var placeholder = (await b.HideAsync(chat, secret.Id, CancellationToken.None)).Ok();
        Assert.True(placeholder.IsHidden);
        Assert.Empty(placeholder.Text);
        Assert.True((await b.HideAsync(chat, secret.Id, CancellationToken.None)).IsSuccess);

        // The numbering keeps no gaps for the reader; the author still sees the text.
        var mine = (await b.GetMessagesAsync(chat, null, null, 50, CancellationToken.None)).Ok().Messages;
        Assert.Equal([1L, 2L], mine.Select(static m => m.Sequence));
        Assert.Equal([true, false], mine.Select(static m => m.IsHidden));
        Assert.Equal("Секрет", (await a.GetMessagesAsync(chat, null, null, 50, CancellationToken.None)).Ok().Messages[0].Text);
        Assert.Equal(1, Assert.Single(await b.ListAsync(CancellationToken.None)).UnreadCount);

        // Hiding the newest message shows the one before it in the list.
        await b.HideAsync(chat, last.Id, CancellationToken.None);
        Assert.Null(Assert.Single(await b.ListAsync(CancellationToken.None)).LastMessage);
        Assert.Equal("Останнє", Assert.Single(await a.ListAsync(CancellationToken.None)).LastMessage?.Text);

        // System notes cannot be hidden.
        var group = (await a.CreateGroupAsync(null, [_world.EmployeeB.Id], CancellationToken.None)).Ok();
        var note = (await b.GetMessagesAsync(group, null, null, 50, CancellationToken.None)).Ok().Messages[0];
        Assert.Equal(ErrorType.Validation, (await b.HideAsync(group, note.Id, CancellationToken.None)).Failure());
    }

    /// <summary>
    /// Builds a message with a new client identifier.
    /// </summary>
    /// <param name="body">The HTML.</param>
    /// <param name="files">The attachments.</param>
    /// <returns>The message.</returns>
    private static SendChatMessage Text(string body, params FileUpload[] files) => new(Guid.NewGuid(), body, files);

    /// <summary>
    /// Builds an uploaded file of a few bytes.
    /// </summary>
    /// <param name="name">The file name.</param>
    /// <returns>The upload.</returns>
    private static FileUpload File(string name) => new(new MemoryStream([1, 2, 3, 4]), name, "application/octet-stream", 4);
}
