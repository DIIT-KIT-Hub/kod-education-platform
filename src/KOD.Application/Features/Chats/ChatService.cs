using KOD.Application.Abstractions;
using KOD.Application.Common.Access;
using KOD.Application.Common.Files;
using KOD.Application.Common.Models;
using KOD.Application.Common.Results;
using KOD.Application.Common.Text;
using KOD.Application.Features.Notifications;
using KOD.Domain.Access;
using KOD.Domain.Chats;
using KOD.Domain.Common;
using KOD.Domain.Localization;
using KOD.Domain.Media;
using KOD.Domain.News;
using KOD.Domain.Notifications;
using KOD.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace KOD.Application.Features.Chats;

/// <summary>
/// Default <see cref="IChatService"/>.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="currentUser">The current user.</param>
/// <param name="sanitizer">Cleans the HTML of messages.</param>
/// <param name="attachmentStore">Checks and stores the files of messages.</param>
/// <param name="files">The private file storage.</param>
/// <param name="events">Delivers changes to the members' pages.</param>
/// <param name="notifier">Collects new messages in the notifications of the members.</param>
/// <param name="timeProvider">The clock.</param>
internal sealed class ChatService(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IRichTextSanitizer sanitizer,
    AttachmentStore attachmentStore,
    IFileStorage files,
    IChatEvents events,
    INotifier notifier,
    TimeProvider timeProvider) : IChatService
{
    /// <summary>
    /// The storage folder of chat files.
    /// </summary>
    private const string StorageCategory = "chats";

    /// <summary>
    /// How many times a message is numbered again when another one took the number first.
    /// </summary>
    private const int SendAttempts = 5;

    /// <summary>
    /// The largest number of messages forwarded at once.
    /// </summary>
    private const int MaxForwarded = 100;

    /// <summary>
    /// The length of the text of a message in the chat list.
    /// </summary>
    private const int PreviewLength = 160;

    /// <summary>
    /// The largest number of chats in the list.
    /// </summary>
    private const int MaxChats = 500;

    /// <summary>
    /// Gets the current user when they may use chats.
    /// </summary>
    private Guid? Me => currentUser.Has(Permissions.Chats.Use) ? currentUser.Id : null;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatSummary>> ListAsync(CancellationToken cancellationToken)
    {
        if (Me is not { } me)
        {
            return [];
        }

        // A one-to-one chat appears for the other person with its first message, and leaves the list of a person who
        // deleted it for themselves until a newer message arrives.
        var rows = await (
                from m in db.ChatMembers
                where m.UserId == me
                join c in db.Chats on m.ChatId equals c.Id
                where c.Kind == ChatKind.Group || c.LastSequence > 0 || c.CreatedById == me
                where c.Kind == ChatKind.Group || m.ClearedSequence == 0 || c.LastSequence > m.ClearedSequence
                orderby c.LastActivityAt descending
                select new
                {
                    c.Id,
                    c.Kind,
                    c.Title,
                    c.LastSequence,
                    c.LastActivityAt,
                    m.LastReadSequence,
                    MemberCount = db.ChatMembers.Count(x => x.ChatId == c.Id),
                    Unread = db.ChatMessages.Count(x => x.ChatId == c.Id
                        && x.Sequence > m.LastReadSequence
                        && x.AuthorId != me
                        && x.Kind == ChatMessageKind.Text
                        && !x.IsDeleted
                        && !db.HiddenChatMessages.Any(h => h.MessageId == x.Id && h.UserId == me)),
                    Members = db.ChatMembers
                        .Where(x => x.ChatId == c.Id && x.UserId != me)
                        .OrderBy(static x => x.JoinedAt)
                        .Take(4)
                        .Select(static x => new PersonRow(x.UserId, x.User!.LastName, x.User.FirstName, x.User.MiddleName, x.User.PhotoId))
                        .ToList(),
                    Last = db.ChatMessages
                        .Where(x => x.ChatId == c.Id
                            && x.Sequence > m.ClearedSequence
                            && !db.HiddenChatMessages.Any(h => h.MessageId == x.Id && h.UserId == me))
                        .OrderByDescending(static x => x.Sequence)
                        .Select(static x => new
                        {
                            Author = new PersonRow(x.AuthorId, x.Author!.LastName, x.Author.FirstName, x.Author.MiddleName, x.Author.PhotoId),
                            x.Kind,
                            x.Event,
                            Text = x.Text.Length > PreviewLength ? x.Text.Substring(0, PreviewLength) : x.Text,
                            x.IsDeleted,
                            Attachments = x.Attachments.Count,
                            x.CreatedAt,
                        })
                        .FirstOrDefault(),
                })
            .AsNoTracking()
            .Take(MaxChats)
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(static r => new ChatSummary(
            r.Id,
            r.Kind,
            r.Title,
            r.Members.ConvertAll(static p => p.ToRef()),
            r.MemberCount,
            r.Last is { } last ? new ChatPreview(last.Author.ToRef(), last.Kind, last.Event, last.Text, last.IsDeleted, last.Attachments, last.CreatedAt) : null,
            r.LastActivityAt,
            r.LastSequence,
            r.LastReadSequence,
            r.Unread));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserLookupItem>> SearchPeopleAsync(string? term, CancellationToken cancellationToken)
    {
        var words = (term ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (Me is not { } me || words.Length == 0 || string.Join(' ', words).Length < 2)
        {
            return [];
        }

        // Every word must match a part of the name or the e-mail ("Іван Петренко" finds "Петренко Іван").
        var people = ChatUsers().Where(u => u.Id != me);
        foreach (var word in words.Take(4))
        {
            people = people.Where(u => u.LastName.Contains(word) || u.FirstName.Contains(word) || (u.MiddleName != null && u.MiddleName.Contains(word)) || u.Email!.Contains(word));
        }

        return await people
            .AsNoTracking()
            .OrderBy(static u => u.LastName).ThenBy(static u => u.FirstName)
            .Take(20)
            .Select(static u => new UserLookupItem(
                u.Id,
                u.LastName + " " + u.FirstName + (u.MiddleName != null ? " " + u.MiddleName : string.Empty),
                u.Email!,
                StoredText.PositionInUnit(
                    u.Positions.Where(static p => p.IsPrimary).Select(static p => p.Title).FirstOrDefault(),
                    u.Positions.Where(static p => p.IsPrimary).Select(static p => p.Role!.Name).FirstOrDefault(),
                    u.Positions.Where(static p => p.IsPrimary).Select(static p => p.OrganizationUnit!.Name).FirstOrDefault()),
                u.PhotoId))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<ChatDetails>> GetAsync(Guid chatId, CancellationToken cancellationToken)
    {
        if (Me is not { } me)
        {
            return NotFound();
        }

        var chat = await db.Chats
            .AsNoTracking()
            .Where(c => c.Id == chatId && c.Members.Any(m => m.UserId == me))
            .Select(static c => new { c.Id, c.Kind, c.Title, c.LastSequence })
            .FirstOrDefaultAsync(cancellationToken);
        if (chat is null)
        {
            return NotFound();
        }

        var members = await db.ChatMembers
            .AsNoTracking()
            .Where(m => m.ChatId == chatId)
            .Select(static m => new
            {
                Person = new PersonRow(m.UserId, m.User!.LastName, m.User.FirstName, m.User.MiddleName, m.User.PhotoId),
                m.Role,
                m.LastReadSequence,
                m.User.IsActive,
                Title = m.User.Positions.Where(static p => p.IsPrimary).Select(static p => p.Title).FirstOrDefault(),
                RoleName = m.User.Positions.Where(static p => p.IsPrimary).Select(static p => p.Role!.Name).FirstOrDefault(),
                UnitName = m.User.Positions.Where(static p => p.IsPrimary).Select(static p => p.OrganizationUnit!.Name).FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        var list = members
            .OrderByDescending(static m => m.Role)
            .ThenBy(static m => m.Person.LastName, StringComparer.CurrentCulture)
            .ThenBy(static m => m.Person.FirstName, StringComparer.CurrentCulture)
            .Select(static m => new ChatMemberDto(m.Person.ToRef(), m.Role, StoredText.PositionInUnit(m.Title, m.RoleName, m.UnitName), m.LastReadSequence, m.IsActive))
            .ToList();
        var myRole = members.First(m => m.Person.Id == me).Role;
        return new ChatDetails(chat.Id, chat.Kind, chat.Title, list, chat.LastSequence, myRole);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>?> GetMemberIdsAsync(Guid chatId, CancellationToken cancellationToken)
    {
        if (Me is not { } me)
        {
            return null;
        }

        var members = await MemberIdsAsync(chatId, cancellationToken);
        return members.Contains(me) ? members : null;
    }

    /// <inheritdoc />
    public async Task<Result<ChatMessagePage>> GetMessagesAsync(Guid chatId, long? before, long? after, int limit, CancellationToken cancellationToken)
    {
        if (Me is not { } me)
        {
            return NotFound();
        }

        var chat = await (
                from m in db.ChatMembers
                where m.ChatId == chatId && m.UserId == me
                join c in db.Chats on m.ChatId equals c.Id
                select new { c.LastSequence, m.ClearedSequence })
            .FirstOrDefaultAsync(cancellationToken);
        if (chat is null)
        {
            return NotFound();
        }

        limit = Math.Clamp(limit, 1, IChatService.MaxPageSize);
        var messages = db.ChatMessages.Where(m => m.ChatId == chatId && m.Sequence > chat.ClearedSequence);
        List<ChatMessageDto> page;
        if (before is null && after is { } from)
        {
            page = await LoadAsync(messages.Where(m => m.Sequence > from).OrderBy(static m => m.Sequence).Take(limit + 1), me, cancellationToken);
        }
        else
        {
            var upTo = before ?? long.MaxValue;
            page = await LoadAsync(messages.Where(m => m.Sequence < upTo).OrderByDescending(static m => m.Sequence).Take(limit + 1), me, cancellationToken);
        }

        var hasMore = page.Count > limit;
        var ordered = page.OrderBy(static m => m.Sequence).ToList();
        if (hasMore)
        {
            // The extra message only tells that more exist: drop it at the far end of the requested direction.
            ordered.RemoveAt(before is null && after is not null ? ordered.Count - 1 : 0);
        }

        return new ChatMessagePage(ordered, hasMore, chat.LastSequence);
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> OpenDirectAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (Me is not { } me)
        {
            return Error.Forbidden();
        }

        if (userId == me)
        {
            return Error.Validation(Texts.T("Не можна почати чат із самим собою."));
        }

        var key = Chat.DirectKeyOf(me, userId);
        var existing = await db.Chats.Where(c => c.DirectKey == key).Select(static c => (Guid?)c.Id).FirstOrDefaultAsync(cancellationToken);
        if (existing is { } id)
        {
            return id;
        }

        if (!await ChatUsers().AnyAsync(u => u.Id == userId, cancellationToken))
        {
            return Error.Validation(Texts.T("Цій людині не можна написати: обліковий запис заблоковано або чати для неї вимкнено."));
        }

        var chat = Chat.Direct(me, userId, timeProvider.GetUtcNow());
        db.Chats.Add(chat);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The other person opened the same chat at the same moment.
            db.DiscardChanges();
            return await db.Chats.Where(c => c.DirectKey == key).Select(static c => c.Id).FirstAsync(cancellationToken);
        }

        return chat.Id;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> CreateGroupAsync(string? title, IReadOnlyCollection<Guid> memberIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(memberIds);
        if (Me is not { } me)
        {
            return Error.Forbidden();
        }

        var invalid = await CheckPeopleAsync(memberIds, cancellationToken);
        if (invalid is not null)
        {
            return invalid;
        }

        try
        {
            var moment = timeProvider.GetUtcNow();
            var chat = Chat.Group(me, title, memberIds, moment);
            db.Chats.Add(chat);
            var created = new ChatMessage(chat.Id, chat.NextSequence(moment), me, ChatEvent.Created, chat.Title);
            db.ChatMessages.Add(created);
            chat.MemberOf(me)!.MarkRead(created.Sequence);
            await db.SaveChangesAsync(cancellationToken);
            events.ChatChanged([.. chat.Members.Select(static m => m.UserId)], chat.Id);
            return chat.Id;
        }
        catch (DomainException exception)
        {
            return Error.Validation(exception.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Result> RenameAsync(Guid chatId, string? title, CancellationToken cancellationToken)
    {
        Task<ChatChange?> Rename(Chat chat, Guid actor)
        {
            chat.Rename(title);
            return Task.FromResult<ChatChange?>(new ChatChange(ChatEvent.Renamed, chat.Title, []));
        }

        var result = await ChangeAsync(chatId, Rename, cancellationToken);
        return result.IsSuccess ? Result.Success() : result.Error;
    }

    /// <inheritdoc />
    public async Task<Result> AddMembersAsync(Guid chatId, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        var invalid = await CheckPeopleAsync(userIds, cancellationToken);
        if (invalid is not null)
        {
            return invalid;
        }

        async Task<ChatChange?> Add(Chat chat, Guid actor)
        {
            var added = chat.AddMembers(userIds, timeProvider.GetUtcNow());
            if (added.Count == 0)
            {
                return null;
            }

            return new ChatChange(ChatEvent.MembersAdded, await NamesAsync(added, cancellationToken), []);
        }

        var result = await ChangeAsync(chatId, Add, cancellationToken);
        return result.IsSuccess ? Result.Success() : result.Error;
    }

    /// <inheritdoc />
    public async Task<Result> RemoveMemberAsync(Guid chatId, Guid userId, CancellationToken cancellationToken)
    {
        if (Me is not { } me)
        {
            return NotFound();
        }

        var members = await MemberIdsAsync(chatId, cancellationToken);
        if (!members.Contains(me))
        {
            return NotFound();
        }

        if (members.Count == 1 && userId == me)
        {
            // The last member leaves: nothing of the chat is kept.
            var chat = await db.Chats.FirstAsync(c => c.Id == chatId, cancellationToken);
            if (chat.Kind != ChatKind.Group)
            {
                return Error.Validation(Texts.T("Це можна зробити лише в груповому чаті."));
            }

            await RemoveChatAsync(chat, [userId], cancellationToken);
            return Result.Success();
        }

        var details = userId == me ? null : await NamesAsync([userId], cancellationToken);
        Task<ChatChange?> Remove(Chat chat, Guid actor)
        {
            chat.RemoveMember(actor, userId);
            return Task.FromResult<ChatChange?>(new ChatChange(userId == me ? ChatEvent.MemberLeft : ChatEvent.MemberRemoved, details, [userId]));
        }

        var result = await ChangeAsync(chatId, Remove, cancellationToken);
        return result.IsSuccess ? Result.Success() : result.Error;
    }

    /// <inheritdoc />
    public async Task<Result> ClearHistoryAsync(Guid chatId, CancellationToken cancellationToken)
    {
        if (Me is not { } me)
        {
            return NotFound();
        }

        var member = await db.ChatMembers.FirstOrDefaultAsync(m => m.ChatId == chatId && m.UserId == me, cancellationToken);
        if (member is null)
        {
            return NotFound();
        }

        // A message sent meanwhile takes a newer number and stays visible.
        var last = await db.Chats.Where(c => c.Id == chatId).Select(static c => c.LastSequence).FirstAsync(cancellationToken);
        var read = member.LastReadSequence;
        member.ClearHistory(last);

        // Messages hidden one by one are all behind the cleared position now.
        db.HiddenChatMessages.RemoveRange(await db.HiddenChatMessages.Where(h => h.ChatId == chatId && h.UserId == me).ToListAsync(cancellationToken));
        await db.SaveChangesAsync(cancellationToken);

        events.HistoryCleared([me], chatId);
        if (member.LastReadSequence > read)
        {
            events.Read(await MemberIdsAsync(chatId, cancellationToken), chatId, me, member.LastReadSequence);
        }

        await notifier.MarkReadAsync(me, chatId, cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> DeleteChatAsync(Guid chatId, CancellationToken cancellationToken)
    {
        if (Me is not { } me)
        {
            return NotFound();
        }

        var chat = await db.Chats.Include(static c => c.Members).FirstOrDefaultAsync(c => c.Id == chatId, cancellationToken);
        if (chat?.MemberOf(me) is not { } member)
        {
            return NotFound();
        }

        if (chat.Kind == ChatKind.Group && member.Role != ChatMemberRole.Owner)
        {
            return Error.Forbidden(Texts.T("Видалити груповий чат для всіх може лише його власник."));
        }

        await RemoveChatAsync(chat, [.. chat.Members.Select(static m => m.UserId)], cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<ChatMessageDto>> SendAsync(Guid chatId, SendChatMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (Me is not { } me)
        {
            return NotFound();
        }

        if (message.ClientId == Guid.Empty)
        {
            return Error.Validation(Texts.T("Повідомлення не має ідентифікатора."));
        }

        var members = await MemberIdsAsync(chatId, cancellationToken);
        if (!members.Contains(me))
        {
            return NotFound();
        }

        // A message resent after a lost answer is already there.
        if (await FindSentAsync(chatId, me, message.ClientId, cancellationToken) is { } sent)
        {
            return sent;
        }

        var body = sanitizer.SanitizeMessage(message.Body ?? string.Empty);
        var text = sanitizer.ToText(body);
        var stored = new List<string>();
        try
        {
            var attachments = await StoreAsync(message.Files, stored, cancellationToken);
            if (!attachments.IsSuccess)
            {
                await DeleteFilesAsync(stored, cancellationToken);
                return attachments.Error;
            }

            for (var attempt = 1; ; attempt++)
            {
                var chat = await db.Chats.FirstAsync(c => c.Id == chatId, cancellationToken);
                var member = await db.ChatMembers.FirstOrDefaultAsync(m => m.ChatId == chatId && m.UserId == me, cancellationToken);
                if (member is null)
                {
                    await DeleteFilesAsync(stored, cancellationToken);
                    return NotFound();
                }

                var added = new ChatMessage(chatId, chat.NextSequence(timeProvider.GetUtcNow()), me, message.ClientId, body, text, attachments.Value);
                db.ChatMessages.Add(added);
                member.MarkRead(added.Sequence);
                try
                {
                    await db.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException) when (attempt < SendAttempts)
                {
                    // Another message took the number: number this one again.
                    db.DiscardChanges();
                    continue;
                }
                catch (DbUpdateException exception) when (exception is not DbUpdateConcurrencyException)
                {
                    // The same message sent twice at once: the other request saved it.
                    db.DiscardChanges();
                    if (await FindSentAsync(chatId, me, message.ClientId, cancellationToken) is { } twin)
                    {
                        await DeleteFilesAsync(stored, cancellationToken);
                        return twin;
                    }

                    throw;
                }

                var dto = (await LoadAsync(db.ChatMessages.Where(m => m.Id == added.Id), null, cancellationToken))[0];
                var recipients = await MemberIdsAsync(chatId, cancellationToken);
                events.MessageAdded(recipients, dto);

                // One unread notification per chat collects its new messages.
                var draft = new NotificationDraft(NotificationKind.ChatMessage, me, chatId, chat.Title ?? dto.Author.FullName, text.Length > 0 ? text : null, $"/chats/{chatId}") { Collect = true };
                await notifier.NotifyAsync(draft, recipients, cancellationToken);
                return dto;
            }
        }
        catch (DomainException exception)
        {
            await DeleteFilesAsync(stored, cancellationToken);
            return Error.Validation(exception.Message);
        }
        catch
        {
            await DeleteFilesAsync(stored, cancellationToken);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ChatMessageDto>>> ForwardAsync(Guid chatId, Guid sourceChatId, IReadOnlyCollection<Guid> messageIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(messageIds);
        if (Me is not { } me)
        {
            return NotFound();
        }

        var ids = messageIds.Distinct().ToList();
        if (ids.Count == 0 || ids.Count > MaxForwarded)
        {
            return Error.Validation(Texts.T("Оберіть від 1 до {0} повідомлень.", MaxForwarded));
        }

        // Both chats must be the user's; only messages the user still sees can be forwarded.
        var cleared = await db.ChatMembers
            .Where(m => m.ChatId == sourceChatId && m.UserId == me)
            .Select(static m => (long?)m.ClearedSequence)
            .FirstOrDefaultAsync(cancellationToken);
        if (cleared is null || !(await MemberIdsAsync(chatId, cancellationToken)).Contains(me))
        {
            return NotFound();
        }

        var originals = await db.ChatMessages
            .AsNoTracking()
            .Include(static m => m.Attachments)
            .Where(m => m.ChatId == sourceChatId
                && ids.Contains(m.Id)
                && m.Kind == ChatMessageKind.Text
                && !m.IsDeleted
                && m.Sequence > cleared.Value
                && !db.HiddenChatMessages.Any(h => h.MessageId == m.Id && h.UserId == me))
            .OrderBy(static m => m.Sequence)
            .ToListAsync(cancellationToken);
        if (originals.Count != ids.Count)
        {
            return Error.Validation(Texts.T("Деякі з цих повідомлень не можна переслати."));
        }

        var stored = new List<string>();
        try
        {
            var copies = new List<List<ChatAttachmentCopy>>();
            foreach (var original in originals)
            {
                var copied = new List<ChatAttachmentCopy>();
                foreach (var attachment in original.Attachments.OrderBy(static a => a.Position))
                {
                    var key = await CopyFileAsync(attachment.StorageKey, stored, cancellationToken);
                    var thumbnail = attachment.ThumbnailKey is { } small ? await CopyFileAsync(small, stored, cancellationToken) : null;
                    copied.Add(new ChatAttachmentCopy(attachment, key, thumbnail));
                }

                copies.Add(copied);
            }

            for (var attempt = 1; ; attempt++)
            {
                var chat = await db.Chats.FirstAsync(c => c.Id == chatId, cancellationToken);
                var member = await db.ChatMembers.FirstOrDefaultAsync(m => m.ChatId == chatId && m.UserId == me, cancellationToken);
                if (member is null)
                {
                    await DeleteFilesAsync(stored, cancellationToken);
                    return NotFound();
                }

                var added = new List<ChatMessage>();
                for (var i = 0; i < originals.Count; i++)
                {
                    var original = originals[i];
                    var attachments = copies[i].ConvertAll(static c => c.ToAttachment());
                    var message = new ChatMessage(chatId, chat.NextSequence(timeProvider.GetUtcNow()), me, Guid.NewGuid(), original.Body, original.Text, attachments);
                    message.MarkForwarded(original.ForwardedFromId ?? original.AuthorId);
                    db.ChatMessages.Add(message);
                    added.Add(message);
                }

                member.MarkRead(added[^1].Sequence);
                try
                {
                    await db.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException) when (attempt < SendAttempts)
                {
                    // Another message took a number: number these again.
                    db.DiscardChanges();
                    continue;
                }

                var addedIds = added.ConvertAll(static m => m.Id);
                var dtos = await LoadAsync(db.ChatMessages.Where(m => addedIds.Contains(m.Id)).OrderBy(static m => m.Sequence), null, cancellationToken);
                var recipients = await MemberIdsAsync(chatId, cancellationToken);
                foreach (var dto in dtos)
                {
                    events.MessageAdded(recipients, dto);
                }

                var last = dtos[^1];
                var draft = new NotificationDraft(NotificationKind.ChatMessage, me, chatId, chat.Title ?? last.Author.FullName, last.Text.Length > 0 ? last.Text : null, $"/chats/{chatId}") { Collect = true };
                await notifier.NotifyAsync(draft, recipients, cancellationToken);
                return dtos;
            }
        }
        catch (DomainException exception)
        {
            await DeleteFilesAsync(stored, cancellationToken);
            return Error.Validation(exception.Message);
        }
        catch
        {
            await DeleteFilesAsync(stored, cancellationToken);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Result<ChatMessageDto>> EditAsync(Guid chatId, Guid messageId, string? body, CancellationToken cancellationToken)
    {
        var found = await OwnMessageAsync(chatId, messageId, Texts.T("Редагувати можна лише власні повідомлення."), cancellationToken);
        if (!found.IsSuccess)
        {
            return found.Error;
        }

        try
        {
            var html = sanitizer.SanitizeMessage(body ?? string.Empty);
            found.Value.Edit(html, sanitizer.ToText(html), timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DomainException exception)
        {
            return Error.Validation(exception.Message);
        }

        return await PublishChangedAsync(chatId, messageId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(Guid chatId, Guid messageId, CancellationToken cancellationToken)
    {
        var found = await OwnMessageAsync(chatId, messageId, Texts.T("Видаляти можна лише власні повідомлення."), cancellationToken);
        if (!found.IsSuccess)
        {
            return found.Error;
        }

        IReadOnlyList<ChatAttachment> removed;
        try
        {
            removed = found.Value.Delete();
        }
        catch (DomainException exception)
        {
            return Error.Validation(exception.Message);
        }

        db.ChatAttachments.RemoveRange(removed);
        db.ChatReactions.RemoveRange(await db.ChatReactions.Where(r => r.MessageId == messageId).ToListAsync(cancellationToken));
        await db.SaveChangesAsync(cancellationToken);
        await DeleteFilesAsync(removed.SelectMany(static a => new[] { a.StorageKey, a.ThumbnailKey }).OfType<string>(), cancellationToken);

        var published = await PublishChangedAsync(chatId, messageId, cancellationToken);
        return published.IsSuccess ? Result.Success() : published.Error;
    }

    /// <inheritdoc />
    public async Task<Result<ChatMessageDto>> HideAsync(Guid chatId, Guid messageId, CancellationToken cancellationToken)
    {
        if (Me is not { } me || !(await MemberIdsAsync(chatId, cancellationToken)).Contains(me))
        {
            return NotFound();
        }

        var kind = await db.ChatMessages
            .Where(m => m.Id == messageId && m.ChatId == chatId)
            .Select(static m => (ChatMessageKind?)m.Kind)
            .FirstOrDefaultAsync(cancellationToken);
        if (kind is null)
        {
            return Error.NotFound(Texts.T("Повідомлення не знайдено."));
        }

        if (kind != ChatMessageKind.Text)
        {
            return Error.Validation(Texts.T("Це повідомлення не можна видалити."));
        }

        if (!await db.HiddenChatMessages.AnyAsync(h => h.MessageId == messageId && h.UserId == me, cancellationToken))
        {
            db.HiddenChatMessages.Add(new HiddenChatMessage(chatId, messageId, me, timeProvider.GetUtcNow()));
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Another page of the user hid it at the same moment.
                db.DiscardChanges();
            }
        }

        // The user's other pages replace the message with the placeholder too.
        var placeholder = (await LoadAsync(db.ChatMessages.Where(m => m.Id == messageId), me, cancellationToken))[0];
        events.MessageChanged([me], placeholder);
        return placeholder;
    }

    /// <inheritdoc />
    public async Task<Result<ChatMessageDto>> ReactAsync(Guid chatId, Guid messageId, ReactionKind kind, CancellationToken cancellationToken)
    {
        if (Me is not { } me || !(await MemberIdsAsync(chatId, cancellationToken)).Contains(me))
        {
            return NotFound();
        }

        if (!Enum.IsDefined(kind)
            || !await db.ChatMessages.AnyAsync(m => m.Id == messageId && m.ChatId == chatId && m.Kind == ChatMessageKind.Text && !m.IsDeleted, cancellationToken))
        {
            return Error.NotFound(Texts.T("Повідомлення не знайдено."));
        }

        var existing = await db.ChatReactions.FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == me, cancellationToken);
        if (existing is null)
        {
            db.ChatReactions.Add(new ChatReaction(messageId, me, kind, timeProvider.GetUtcNow()));
        }
        else if (existing.Kind == kind)
        {
            db.ChatReactions.Remove(existing);
        }
        else
        {
            existing.Change(kind, timeProvider.GetUtcNow());
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A double click from two tabs: the other request has already saved the reaction.
            db.DiscardChanges();
        }

        return await PublishChangedAsync(chatId, messageId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> MarkReadAsync(Guid chatId, long sequence, CancellationToken cancellationToken)
    {
        if (Me is not { } me)
        {
            return NotFound();
        }

        var member = await db.ChatMembers.FirstOrDefaultAsync(m => m.ChatId == chatId && m.UserId == me, cancellationToken);
        if (member is null)
        {
            return NotFound();
        }

        var last = await db.Chats.Where(c => c.Id == chatId).Select(static c => c.LastSequence).FirstAsync(cancellationToken);
        if (member.MarkRead(Math.Min(sequence, last)))
        {
            await db.SaveChangesAsync(cancellationToken);
            events.Read(await MemberIdsAsync(chatId, cancellationToken), chatId, me, member.LastReadSequence);
        }

        // Read to the end: the notification about the chat is done.
        if (member.LastReadSequence >= last)
        {
            await notifier.MarkReadAsync(me, chatId, cancellationToken);
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<DownloadableFile>> OpenAttachmentAsync(Guid chatId, Guid attachmentId, bool thumbnail, CancellationToken cancellationToken)
    {
        if (Me is not { } me)
        {
            return NotFound();
        }

        var attachment = await db.ChatAttachments
            .AsNoTracking()
            .Where(a => a.Id == attachmentId && a.ChatId == chatId && db.ChatMembers.Any(m => m.ChatId == chatId && m.UserId == me))
            .FirstOrDefaultAsync(cancellationToken);
        if (attachment is null)
        {
            return Error.NotFound(Texts.T("Файл не знайдено."));
        }

        var key = thumbnail && attachment.ThumbnailKey is { } small ? small : attachment.StorageKey;
        var name = attachment.Kind == ChatAttachmentKind.Image ? Path.ChangeExtension(attachment.FileName, ".avif") : attachment.FileName;
        return new DownloadableFile(await files.OpenReadAsync(key, cancellationToken), name, attachment.ContentType);
    }

    /// <summary>
    /// Builds the address of an attachment.
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="attachmentId">The attachment.</param>
    /// <param name="thumbnail">Whether the thumbnail is meant.</param>
    /// <returns>The address.</returns>
    private static string AttachmentUrl(Guid chatId, Guid attachmentId, bool thumbnail)
        => $"/api/v1/chats/{chatId}/attachments/{attachmentId}{(thumbnail ? "/thumb" : string.Empty)}";

    /// <summary>
    /// The error of a chat or message the current user cannot see.
    /// </summary>
    /// <returns>The error.</returns>
    private static Error NotFound() => Error.NotFound(Texts.T("Чат не знайдено."));

    /// <summary>
    /// Selects the active accounts that may use chats (role permissions and individual grants, minus denials).
    /// </summary>
    /// <returns>The query.</returns>
    private IQueryable<User> ChatUsers() => db.UsersWith(Permissions.Chats.Use);

    /// <summary>
    /// Checks that everyone may be added to a chat.
    /// </summary>
    /// <param name="userIds">The people.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An error, or <see langword="null"/> when all of them may chat.</returns>
    private async Task<Error?> CheckPeopleAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count > Chat.MaxMembers)
        {
            return Error.Validation(Texts.T("У груповому чаті може бути не більше {0} учасників.", Chat.MaxMembers));
        }

        var allowed = await ChatUsers().CountAsync(u => ids.Contains(u.Id), cancellationToken);
        return allowed == ids.Count
            ? null
            : Error.Validation(Texts.T("Цій людині не можна написати: обліковий запис заблоковано або чати для неї вимкнено."));
    }

    /// <summary>
    /// Lists the members of a chat.
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The member identifiers (empty when the chat does not exist).</returns>
    private async Task<List<Guid>> MemberIdsAsync(Guid chatId, CancellationToken cancellationToken)
        => await db.ChatMembers.Where(m => m.ChatId == chatId).Select(static m => m.UserId).ToListAsync(cancellationToken);

    /// <summary>
    /// Joins the full names of people for a system note.
    /// </summary>
    /// <param name="userIds">The people.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The names, comma separated.</returns>
    private async Task<string> NamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        var names = await db.Users
            .Where(u => userIds.Contains(u.Id))
            .OrderBy(static u => u.LastName).ThenBy(static u => u.FirstName)
            .Select(static u => u.LastName + " " + u.FirstName)
            .ToListAsync(cancellationToken);
        return string.Join(", ", names);
    }

    /// <summary>
    /// Deletes a chat with its messages and files and tells its former members.
    /// </summary>
    /// <param name="chat">The tracked chat.</param>
    /// <param name="members">The members to tell.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when deleted.</returns>
    private async Task RemoveChatAsync(Chat chat, IReadOnlyCollection<Guid> members, CancellationToken cancellationToken)
    {
        var keys = await db.ChatAttachments
            .Where(a => a.ChatId == chat.Id)
            .Select(static a => new { a.StorageKey, a.ThumbnailKey })
            .ToListAsync(cancellationToken);
        db.Chats.Remove(chat);
        await db.SaveChangesAsync(cancellationToken);
        await DeleteFilesAsync(keys.SelectMany(static k => new[] { k.StorageKey, k.ThumbnailKey }).OfType<string>(), cancellationToken);
        events.ChatRemoved(members, chat.Id);

        // The unread notifications about the chat lead nowhere now.
        foreach (var member in members)
        {
            await notifier.MarkReadAsync(member, chat.Id, cancellationToken);
        }
    }

    /// <summary>
    /// Finds a message the current user has already sent with a client identifier.
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="authorId">The author.</param>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The message, or <see langword="null"/>.</returns>
    private async Task<ChatMessageDto?> FindSentAsync(Guid chatId, Guid authorId, Guid clientId, CancellationToken cancellationToken)
    {
        var sent = await LoadAsync(db.ChatMessages.Where(m => m.ChatId == chatId && m.AuthorId == authorId && m.ClientId == clientId), null, cancellationToken);
        return sent.Count > 0 ? sent[0] : null;
    }

    /// <summary>
    /// Changes a group chat and adds the system note about it, numbering the note again when another message took its
    /// number first; then tells the members.
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="change">Applies the change to the tracked chat and describes it; <see langword="null"/> when nothing changed.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The note, or nothing when nothing changed.</returns>
    private async Task<Result<ChatMessageDto?>> ChangeAsync(Guid chatId, Func<Chat, Guid, Task<ChatChange?>> change, CancellationToken cancellationToken)
    {
        if (Me is not { } me)
        {
            return NotFound();
        }

        for (var attempt = 1; ; attempt++)
        {
            var chat = await db.Chats.Include(static c => c.Members).FirstOrDefaultAsync(c => c.Id == chatId, cancellationToken);
            if (chat?.MemberOf(me) is null)
            {
                return NotFound();
            }

            ChatChange? described;
            try
            {
                described = await change(chat, me);
            }
            catch (DomainException exception)
            {
                return Error.Validation(exception.Message);
            }

            if (described is null)
            {
                return Result<ChatMessageDto?>.Success(null);
            }

            var note = new ChatMessage(chat.Id, chat.NextSequence(timeProvider.GetUtcNow()), me, described.Event, described.Details);
            db.ChatMessages.Add(note);
            chat.MemberOf(me)?.MarkRead(note.Sequence);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException) when (attempt < SendAttempts)
            {
                db.DiscardChanges();
                continue;
            }

            var members = chat.Members.Select(static m => m.UserId).ToList();
            var dto = (await LoadAsync(db.ChatMessages.Where(m => m.Id == note.Id), null, cancellationToken))[0];
            if (described.Removed.Count > 0)
            {
                events.ChatRemoved(described.Removed, chat.Id);
            }

            events.ChatChanged(members, chat.Id);
            events.MessageAdded(members, dto);
            return dto;
        }
    }

    /// <summary>
    /// Loads a message of the current user for a change.
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="messageId">The message.</param>
    /// <param name="notOwn">The error when the message is someone else's.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tracked message with its attachments.</returns>
    private async Task<Result<ChatMessage>> OwnMessageAsync(Guid chatId, Guid messageId, string notOwn, CancellationToken cancellationToken)
    {
        if (Me is not { } me || !(await MemberIdsAsync(chatId, cancellationToken)).Contains(me))
        {
            return NotFound();
        }

        var message = await db.ChatMessages
            .Include(static m => m.Attachments)
            .FirstOrDefaultAsync(m => m.Id == messageId && m.ChatId == chatId, cancellationToken);
        if (message is null)
        {
            return Error.NotFound(Texts.T("Повідомлення не знайдено."));
        }

        return message.AuthorId == me ? message : Error.Forbidden(notOwn);
    }

    /// <summary>
    /// Loads a message as it is now and sends it to the members.
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="messageId">The message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The message as it is now.</returns>
    private async Task<Result<ChatMessageDto>> PublishChangedAsync(Guid chatId, Guid messageId, CancellationToken cancellationToken)
    {
        var loaded = await LoadAsync(db.ChatMessages.Where(m => m.Id == messageId && m.ChatId == chatId), null, cancellationToken);
        if (loaded.Count == 0)
        {
            return Error.NotFound(Texts.T("Повідомлення не знайдено."));
        }

        events.MessageChanged(await MemberIdsAsync(chatId, cancellationToken), loaded[0]);
        return loaded[0];
    }

    /// <summary>
    /// Projects messages for display with their authors, attachments and reactions (three queries in all).
    /// </summary>
    /// <param name="messages">The filtered and ordered messages.</param>
    /// <param name="reader">The member whose hidden messages become empty placeholders; <see langword="null"/> for
    /// messages pushed to all members.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The messages in the order of the query.</returns>
    private async Task<List<ChatMessageDto>> LoadAsync(IQueryable<ChatMessage> messages, Guid? reader, CancellationToken cancellationToken)
    {
        var rows = await messages
            .AsNoTracking()
            .Select(static m => new
            {
                m.Id,
                m.ChatId,
                m.Sequence,
                Author = new PersonRow(m.AuthorId, m.Author!.LastName, m.Author.FirstName, m.Author.MiddleName, m.Author.PhotoId),
                ForwardedFrom = m.ForwardedFromId == null
                    ? null
                    : new PersonRow(m.ForwardedFrom!.Id, m.ForwardedFrom.LastName, m.ForwardedFrom.FirstName, m.ForwardedFrom.MiddleName, m.ForwardedFrom.PhotoId),
                m.Kind,
                m.Event,
                m.Body,
                m.Text,
                m.CreatedAt,
                m.EditedAt,
                m.IsDeleted,
                m.ClientId,
            })
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return [];
        }

        var ids = rows.ConvertAll(static r => r.Id);
        var attachments = (await db.ChatAttachments
                .AsNoTracking()
                .Where(a => ids.Contains(a.MessageId))
                .ToListAsync(cancellationToken))
            .OrderBy(static a => a.Position)
            .ToLookup(static a => a.MessageId);
        var reactions = (await db.ChatReactions
                .AsNoTracking()
                .Where(r => ids.Contains(r.MessageId))
                .Select(static r => new { r.MessageId, r.Kind, r.UserId, r.CreatedAt })
                .ToListAsync(cancellationToken))
            .ToLookup(static r => r.MessageId);
        var hidden = reader is { } readerId
            ? (await db.HiddenChatMessages
                .Where(h => h.UserId == readerId && ids.Contains(h.MessageId))
                .Select(static h => h.MessageId)
                .ToListAsync(cancellationToken)).ToHashSet()
            : [];

        var loaded = rows.ConvertAll(r => new ChatMessageDto(
            r.Id,
            r.ChatId,
            r.Sequence,
            r.Author.ToRef(),
            r.Kind,
            r.Event,
            r.Body,
            r.Text,
            r.CreatedAt,
            r.EditedAt,
            r.IsDeleted,
            r.ClientId,
            attachments[r.Id].Select(a => ToDto(r.ChatId, a)).ToList(),
            reactions[r.Id]
                .GroupBy(static x => x.Kind)
                .OrderBy(static g => g.Min(static x => x.CreatedAt))
                .Select(static g => new ChatReactionDto(g.Key, g.OrderBy(static x => x.CreatedAt).Select(static x => x.UserId).ToList()))
                .ToList(),
            ForwardedFrom: r.ForwardedFrom?.ToRef()));
        return loaded.ConvertAll(m => hidden.Contains(m.Id) ? Placeholder(m) : m);

        static ChatMessageDto Placeholder(ChatMessageDto m)
            => m with { Body = string.Empty, Text = string.Empty, EditedAt = null, Attachments = [], Reactions = [], IsHidden = true, ForwardedFrom = null };

        static ChatAttachmentDto ToDto(Guid chatId, ChatAttachment a) => new(
            a.Id,
            a.Kind,
            a.FileName,
            a.ContentType,
            a.Size,
            a.Width,
            a.Height,
            AttachmentUrl(chatId, a.Id, thumbnail: false),
            a.ThumbnailKey is null ? null : AttachmentUrl(chatId, a.Id, thumbnail: true));
    }

    /// <summary>
    /// Checks and stores the files of a message.
    /// </summary>
    /// <param name="uploads">The uploaded files.</param>
    /// <param name="stored">Collects the keys of the stored files (deleted again when sending fails).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The attachments.</returns>
    private async Task<Result<IReadOnlyList<ChatAttachment>>> StoreAsync(IReadOnlyList<FileUpload> uploads, List<string> stored, CancellationToken cancellationToken)
    {
        var result = await attachmentStore.StoreAsync(
            uploads,
            StorageCategory,
            ChatMessage.MaxAttachments,
            static extension => Texts.T("Файли типу «{0}» не можна надсилати в чат.", extension),
            stored,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error;
        }

        return Result<IReadOnlyList<ChatAttachment>>.Success(result.Value.Select(static f => new ChatAttachment(
            f.ThumbnailKey is null ? ChatAttachmentKind.File : ChatAttachmentKind.Image,
            f.FileName,
            f.ContentType,
            f.Size,
            f.StorageKey,
            f.ThumbnailKey,
            f.Width,
            f.Height)).ToList());
    }

    /// <summary>
    /// Copies a stored chat file under a new key (a forwarded message owns its files).
    /// </summary>
    /// <param name="storageKey">The key of the file.</param>
    /// <param name="stored">Collects the keys of the copies (deleted again when forwarding fails).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The key of the copy.</returns>
    private async Task<string> CopyFileAsync(string storageKey, List<string> stored, CancellationToken cancellationToken)
    {
        await using var content = await files.OpenReadAsync(storageKey, cancellationToken);
        var copy = await files.SaveAsync(content, StorageCategory, FileRules.MaxChatFileSize, cancellationToken);
        stored.Add(copy.StorageKey);
        return copy.StorageKey;
    }

    /// <summary>
    /// Deletes stored files; a file that cannot be deleted stays (it is unreachable without its row).
    /// </summary>
    /// <param name="keys">The storage keys.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when deleted.</returns>
    private Task DeleteFilesAsync(IEnumerable<string> keys, CancellationToken cancellationToken) => attachmentStore.DeleteAsync(keys, cancellationToken);

    /// <summary>
    /// The copied files of an attachment of a forwarded message.
    /// </summary>
    /// <param name="Original">The attachment of the original message.</param>
    /// <param name="StorageKey">The key of the copied file.</param>
    /// <param name="ThumbnailKey">The key of the copied thumbnail.</param>
    private sealed record ChatAttachmentCopy(ChatAttachment Original, string StorageKey, string? ThumbnailKey)
    {
        /// <summary>
        /// Builds the attachment of the new message.
        /// </summary>
        /// <returns>The attachment.</returns>
        public ChatAttachment ToAttachment()
            => new(Original.Kind, Original.FileName, Original.ContentType, Original.Size, StorageKey, ThumbnailKey, Original.Width, Original.Height);
    }

    /// <summary>
    /// A change of a chat for its system note.
    /// </summary>
    /// <param name="Event">What happened.</param>
    /// <param name="Details">The new title or the names of the people concerned.</param>
    /// <param name="Removed">The people no longer in the chat.</param>
    private sealed record ChatChange(ChatEvent Event, string? Details, IReadOnlyList<Guid> Removed);

    /// <summary>
    /// A person projected from the database.
    /// </summary>
    /// <param name="Id">The identifier.</param>
    /// <param name="LastName">The last name.</param>
    /// <param name="FirstName">The first name.</param>
    /// <param name="MiddleName">The patronymic.</param>
    /// <param name="PhotoId">The profile photo.</param>
    private sealed record PersonRow(Guid Id, string LastName, string FirstName, string? MiddleName, Guid? PhotoId)
    {
        /// <summary>
        /// Converts the row into a reference.
        /// </summary>
        /// <returns>The reference.</returns>
        public UserRef ToRef() => new(Id, MiddleName is null ? $"{LastName} {FirstName}" : $"{LastName} {FirstName} {MiddleName}", PhotoId);
    }
}
