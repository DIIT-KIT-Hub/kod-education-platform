using KOD.Application.Common.Files;
using KOD.Application.Common.Models;
using KOD.Application.Common.Results;
using KOD.Domain.News;

namespace KOD.Application.Features.Chats;

/// <summary>
/// One-to-one and group chats. Everything is limited to the chats the current user is a member of: another chat
/// "does not exist" (404), whoever asks.
/// </summary>
public interface IChatService
{
    /// <summary>
    /// The default number of messages per page.
    /// </summary>
    const int PageSize = 50;

    /// <summary>
    /// The largest number of messages per page.
    /// </summary>
    const int MaxPageSize = 100;

    /// <summary>
    /// Lists the chats of the current user, the most recently active first.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The chats.</returns>
    Task<IReadOnlyList<ChatSummary>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Finds people to chat with: active accounts that may use chats, by name or e-mail (at least two characters).
    /// </summary>
    /// <param name="term">The search text.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Up to 20 people, the current user excluded.</returns>
    Task<IReadOnlyList<UserLookupItem>> SearchPeopleAsync(string? term, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a chat with its members.
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The chat with its members.</returns>
    Task<Result<ChatDetails>> GetAsync(Guid chatId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the members of a chat the current user is in (e.g. to tell them that the user is typing).
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The members, or <see langword="null"/> when the current user is not a member.</returns>
    Task<IReadOnlyList<Guid>?> GetMemberIdsAsync(Guid chatId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets messages of a chat: the newest page, the page before a number (scrolling up) or the messages after a number
    /// (catching up after a reconnect).
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="before">Return messages with smaller numbers.</param>
    /// <param name="after">Return messages with larger numbers (ignored with <paramref name="before"/>).</param>
    /// <param name="limit">The number of messages.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The messages, oldest first.</returns>
    Task<Result<ChatMessagePage>> GetMessagesAsync(Guid chatId, long? before, long? after, int limit, CancellationToken cancellationToken);

    /// <summary>
    /// Opens the one-to-one chat with a person, creating it on the first call.
    /// </summary>
    /// <param name="userId">The person.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The chat identifier.</returns>
    Task<Result<Guid>> OpenDirectAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a group chat with the current user as its owner.
    /// </summary>
    /// <param name="title">The title, or <see langword="null"/> to show the members' names.</param>
    /// <param name="memberIds">The other members.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The chat identifier.</returns>
    Task<Result<Guid>> CreateGroupAsync(string? title, IReadOnlyCollection<Guid> memberIds, CancellationToken cancellationToken);

    /// <summary>
    /// Renames a group chat (any member).
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="title">The new title, or <see langword="null"/> to show the members' names.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> RenameAsync(Guid chatId, string? title, CancellationToken cancellationToken);

    /// <summary>
    /// Adds people to a group chat (any member).
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="userIds">The people.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> AddMembersAsync(Guid chatId, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);

    /// <summary>
    /// Leaves a group chat (the current user) or removes a person from it (the owner). A chat without members is deleted
    /// with its messages and files.
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="userId">The person.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> RemoveMemberAsync(Guid chatId, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Clears the history of a chat for the current user only (the others keep it). A one-to-one chat also leaves the
    /// user's list until a new message arrives ("delete for me").
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> ClearHistoryAsync(Guid chatId, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a chat for all its members with its messages and files: a one-to-one chat by either person, a group chat
    /// by its owner.
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> DeleteChatAsync(Guid chatId, CancellationToken cancellationToken);

    /// <summary>
    /// Sends a message with formatted text and attachments. Sending again with the same client identifier returns the
    /// message already sent.
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="message">The message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The saved message.</returns>
    Task<Result<ChatMessageDto>> SendAsync(Guid chatId, SendChatMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces the text of the current user's message.
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="messageId">The message.</param>
    /// <param name="body">The HTML of the editor.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The edited message.</returns>
    Task<Result<ChatMessageDto>> EditAsync(Guid chatId, Guid messageId, string? body, CancellationToken cancellationToken);

    /// <summary>
    /// Forwards messages of a chat to another chat (or the same one) as new messages of the current user that name the
    /// original authors; their files are copied, so deleting the originals keeps the copies.
    /// </summary>
    /// <param name="chatId">The chat the messages go to.</param>
    /// <param name="sourceChatId">The chat of the messages.</param>
    /// <param name="messageIds">The messages, forwarded in their order in the source chat.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The new messages.</returns>
    Task<Result<IReadOnlyList<ChatMessageDto>>> ForwardAsync(Guid chatId, Guid sourceChatId, IReadOnlyCollection<Guid> messageIds, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the current user's message for everyone with its files (a placeholder stays).
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="messageId">The message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> DeleteAsync(Guid chatId, Guid messageId, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a message (anyone's, also a deleted placeholder) for the current user only.
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="messageId">The message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The empty placeholder the user sees now.</returns>
    Task<Result<ChatMessageDto>> HideAsync(Guid chatId, Guid messageId, CancellationToken cancellationToken);

    /// <summary>
    /// Sets the current user's emotion on a message; choosing the same emotion again removes it.
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="messageId">The message.</param>
    /// <param name="kind">The emotion.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The message with its reactions.</returns>
    Task<Result<ChatMessageDto>> ReactAsync(Guid chatId, Guid messageId, ReactionKind kind, CancellationToken cancellationToken);

    /// <summary>
    /// Marks a chat as read up to a message (the position only moves forward).
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="sequence">The number of the last read message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> MarkReadAsync(Guid chatId, long sequence, CancellationToken cancellationToken);

    /// <summary>
    /// Opens a file or a photo of a message.
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="attachmentId">The attachment.</param>
    /// <param name="thumbnail">Whether to open the thumbnail of a photo.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The file.</returns>
    Task<Result<DownloadableFile>> OpenAttachmentAsync(Guid chatId, Guid attachmentId, bool thumbnail, CancellationToken cancellationToken);
}
