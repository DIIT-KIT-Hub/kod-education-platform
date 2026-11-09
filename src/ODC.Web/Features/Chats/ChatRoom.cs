using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Features.Chats.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Files;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;
using ODC.Web.Shared.Models;

namespace ODC.Web.Features.Chats;

/// <summary>
/// The rules the chat slices share: membership, numbering, loading messages for a reader, changes of a group chat,
/// the files of messages and their copies.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="catalog">The shared catalogs (who may use chats).</param>
/// <param name="currentUser">The current user.</param>
/// <param name="attachmentStore">Checks and stores the files of messages.</param>
/// <param name="files">The private file storage.</param>
/// <param name="events">Delivers changes to the members' pages.</param>
/// <param name="domainEvents">Raises what the user has seen (their notifications about it are done).</param>
/// <param name="timeProvider">The clock.</param>
internal sealed class ChatRoom(
    OrganizationDbContext db,
    Catalog catalog,
    ICurrentUser currentUser,
    AttachmentStore attachmentStore,
    IFileStorage files,
    IChatEvents events,
    DomainEvents domainEvents,
    TimeProvider timeProvider)
{
    /// <summary>
    /// The default number of messages per page.
    /// </summary>
    public const int PageSize = 50;

    /// <summary>
    /// The largest page a client may ask for.
    /// </summary>
    public const int MaxPageSize = 100;

    /// <summary>
    /// The storage folder of chat files.
    /// </summary>
    public const string StorageCategory = "chats";

    /// <summary>
    /// How many times a message is numbered again when another one took the number first.
    /// </summary>
    public const int SendAttempts = 5;

    /// <summary>
    /// The largest number of messages forwarded at once.
    /// </summary>
    public const int MaxForwarded = 100;

    /// <summary>
    /// The length of the text of a message in the chat list.
    /// </summary>
    public const int PreviewLength = 160;

    /// <summary>
    /// The largest number of chats in the list.
    /// </summary>
    public const int MaxChats = 500;

    /// <summary>
    /// Gets the current user when they may use chats.
    /// </summary>
    public Guid? Me => currentUser.Has(Permissions.Chats.Use) ? currentUser.Id : null;

    /// <summary>
    /// The route group <c>/api/v1/chats</c> (people who may use chats).
    /// </summary>
    /// <param name="api">The API group.</param>
    /// <returns>The group.</returns>
    public static RouteGroupBuilder MapChats(IEndpointRouteBuilder api)
        => api.MapGroup("chats").RequirePermission(Permissions.Chats.Use).WithTags("Chats");

    /// <summary>
    /// Builds the address of an attachment.
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="attachmentId">The attachment.</param>
    /// <param name="thumbnail">Whether the thumbnail is meant.</param>
    /// <returns>The address.</returns>
    public static string AttachmentUrl(Guid chatId, Guid attachmentId, bool thumbnail)
        => $"/api/v1/chats/{chatId}/attachments/{attachmentId}{(thumbnail ? "/thumb" : string.Empty)}";

    /// <summary>
    /// The error of a chat or message the current user cannot see.
    /// </summary>
    /// <returns>The error.</returns>
    public static Error NotFound() => Error.NotFound(Texts.T("Чат не знайдено."));

    /// <summary>
    /// Selects the active accounts that may use chats (role permissions and individual grants, minus denials).
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The query.</returns>
    public Task<IQueryable<Member>> ChatUsersAsync(CancellationToken cancellationToken) => db.MembersWithAsync(catalog, Permissions.Chats.Use, cancellationToken);

    /// <summary>
    /// Checks that everyone may be added to a chat.
    /// </summary>
    /// <param name="userIds">The people.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An error, or <see langword="null"/> when all of them may chat.</returns>
    public async Task<Error?> CheckPeopleAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count > Chat.MaxMembers)
        {
            return Error.Validation(Texts.T("У груповому чаті може бути не більше {0} учасників.", Chat.MaxMembers));
        }

        var allowed = await (await ChatUsersAsync(cancellationToken)).CountAsync(u => ids.Contains(u.Id), cancellationToken);
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
    public async Task<List<Guid>> MemberIdsAsync(Guid chatId, CancellationToken cancellationToken)
        => await db.ChatMembers.Where(m => m.ChatId == chatId).Select(static m => m.UserId).ToListAsync(cancellationToken);

    /// <summary>
    /// Joins the full names of people for a system note.
    /// </summary>
    /// <param name="userIds">The people.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The names, comma separated.</returns>
    public async Task<string> NamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        var names = await db.Members
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
    public async Task RemoveChatAsync(Chat chat, IReadOnlyCollection<Guid> members, CancellationToken cancellationToken)
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
            domainEvents.Raise(new ItemSeen(member, chat.Id));
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
    public async Task<ChatMessageDto?> FindSentAsync(Guid chatId, Guid authorId, Guid clientId, CancellationToken cancellationToken)
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
    public async Task<Result<ChatMessageDto?>> ChangeAsync(Guid chatId, Func<Chat, Guid, Task<ChatChange?>> change, CancellationToken cancellationToken)
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
    public async Task<Result<ChatMessage>> OwnMessageAsync(Guid chatId, Guid messageId, string notOwn, CancellationToken cancellationToken)
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
    public async Task<Result<ChatMessageDto>> PublishChangedAsync(Guid chatId, Guid messageId, CancellationToken cancellationToken)
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
    public async Task<List<ChatMessageDto>> LoadAsync(IQueryable<ChatMessage> messages, Guid? reader, CancellationToken cancellationToken)
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
    public async Task<Result<IReadOnlyList<ChatAttachment>>> StoreAsync(IReadOnlyList<FileUpload> uploads, List<string> stored, CancellationToken cancellationToken)
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
    public async Task<string> CopyFileAsync(string storageKey, List<string> stored, CancellationToken cancellationToken)
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
    public Task DeleteFilesAsync(IEnumerable<string> keys, CancellationToken cancellationToken) => attachmentStore.DeleteAsync(keys, cancellationToken);

    /// <summary>
    /// The copied files of an attachment of a forwarded message.
    /// </summary>
    /// <param name="Original">The attachment of the original message.</param>
    /// <param name="StorageKey">The key of the copied file.</param>
    /// <param name="ThumbnailKey">The key of the copied thumbnail.</param>
    public sealed record ChatAttachmentCopy(ChatAttachment Original, string StorageKey, string? ThumbnailKey)
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
    public sealed record ChatChange(ChatEvent Event, string? Details, IReadOnlyList<Guid> Removed);

    /// <summary>
    /// A person projected from the database.
    /// </summary>
    /// <param name="Id">The identifier.</param>
    /// <param name="LastName">The last name.</param>
    /// <param name="FirstName">The first name.</param>
    /// <param name="MiddleName">The patronymic.</param>
    /// <param name="PhotoId">The profile photo.</param>
    public sealed record PersonRow(Guid Id, string LastName, string FirstName, string? MiddleName, Guid? PhotoId)
    {
        /// <summary>
        /// Converts the row into a reference.
        /// </summary>
        /// <returns>The reference.</returns>
        public UserRef ToRef() => new(Id, MiddleName is null ? $"{LastName} {FirstName}" : $"{LastName} {FirstName} {MiddleName}", PhotoId);
    }
}
