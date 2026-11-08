using ODC.Web.Features.Chats;
using ODC.Web.Features.News.Domain;
using ODC.Web.Shared.Domain.Reactions;
using ODC.Web.Shared.Files;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Models;

namespace ODC.UnitTests.Application;

/// <summary>
/// The chat slices as one user calls them, with the shapes the chat tests read best (one method per slice).
/// </summary>
/// <param name="services">The user's services.</param>
internal sealed class ChatClient(TwoOrganizations.Services services)
{
    /// <summary>Lists the user's chats.</summary>
    /// <param name="cancellationToken">Ignored (the tests use none).</param>
    /// <returns>The chats.</returns>
    public Task<IReadOnlyList<ChatSummary>> ListAsync(CancellationToken cancellationToken) => services.SendAsync(new ListChats.Query());

    /// <summary>Finds people to write to.</summary>
    /// <param name="term">The search text.</param>
    /// <param name="cancellationToken">Ignored.</param>
    /// <returns>The people.</returns>
    public Task<IReadOnlyList<UserLookupItem>> SearchPeopleAsync(string? term, CancellationToken cancellationToken) => services.SendAsync(new SearchChatPeople.Query(term));

    /// <summary>Gets a chat.</summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="cancellationToken">Ignored.</param>
    /// <returns>The chat with its members.</returns>
    public Task<Result<ChatDetails>> GetAsync(Guid chatId, CancellationToken cancellationToken) => services.SendAsync(new GetChat.Query(chatId));

    /// <summary>Gets a page of messages.</summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="before">Older than this number.</param>
    /// <param name="after">Newer than this number.</param>
    /// <param name="limit">The page size.</param>
    /// <param name="cancellationToken">Ignored.</param>
    /// <returns>The page.</returns>
    public Task<Result<ChatMessagePage>> GetMessagesAsync(Guid chatId, long? before, long? after, int limit, CancellationToken cancellationToken)
        => services.SendAsync(new GetChatMessages.Query(chatId, before, after, limit));

    /// <summary>Opens a one-to-one chat.</summary>
    /// <param name="userId">The person.</param>
    /// <param name="cancellationToken">Ignored.</param>
    /// <returns>The chat.</returns>
    public Task<Result<Guid>> OpenDirectAsync(Guid userId, CancellationToken cancellationToken) => services.SendAsync(new OpenDirectChat.Command(userId));

    /// <summary>Creates a group chat.</summary>
    /// <param name="title">The title.</param>
    /// <param name="memberIds">The members.</param>
    /// <param name="cancellationToken">Ignored.</param>
    /// <returns>The chat.</returns>
    public Task<Result<Guid>> CreateGroupAsync(string? title, IReadOnlyCollection<Guid> memberIds, CancellationToken cancellationToken)
        => services.SendAsync(new CreateGroupChat.Command(title, memberIds));

    /// <summary>Renames a group chat.</summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="title">The title.</param>
    /// <param name="cancellationToken">Ignored.</param>
    /// <returns>The result.</returns>
    public Task<Result> RenameAsync(Guid chatId, string? title, CancellationToken cancellationToken) => services.SendAsync(new RenameChat.Command(chatId, title));

    /// <summary>Adds members.</summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="userIds">The people.</param>
    /// <param name="cancellationToken">Ignored.</param>
    /// <returns>The result.</returns>
    public Task<Result> AddMembersAsync(Guid chatId, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
        => services.SendAsync(new AddChatMembers.Command(chatId, userIds));

    /// <summary>Removes a member.</summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="userId">The member.</param>
    /// <param name="cancellationToken">Ignored.</param>
    /// <returns>The result.</returns>
    public Task<Result> RemoveMemberAsync(Guid chatId, Guid userId, CancellationToken cancellationToken) => services.SendAsync(new RemoveChatMember.Command(chatId, userId));

    /// <summary>Clears the history.</summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="cancellationToken">Ignored.</param>
    /// <returns>The result.</returns>
    public Task<Result> ClearHistoryAsync(Guid chatId, CancellationToken cancellationToken) => services.SendAsync(new ClearChatHistory.Command(chatId));

    /// <summary>Deletes a chat.</summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="cancellationToken">Ignored.</param>
    /// <returns>The result.</returns>
    public Task<Result> DeleteChatAsync(Guid chatId, CancellationToken cancellationToken) => services.SendAsync(new DeleteChat.Command(chatId));

    /// <summary>Sends a message.</summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="message">The message.</param>
    /// <param name="cancellationToken">Ignored.</param>
    /// <returns>The message as the members see it.</returns>
    public Task<Result<ChatMessageDto>> SendAsync(Guid chatId, SendChatMessage message, CancellationToken cancellationToken)
        => services.SendAsync(new PostChatMessage.Command(chatId, message));

    /// <summary>Forwards messages.</summary>
    /// <param name="chatId">The target chat.</param>
    /// <param name="sourceChatId">The source chat.</param>
    /// <param name="messageIds">The messages.</param>
    /// <param name="cancellationToken">Ignored.</param>
    /// <returns>The copies in the target chat.</returns>
    public Task<Result<IReadOnlyList<ChatMessageDto>>> ForwardAsync(Guid chatId, Guid sourceChatId, IReadOnlyCollection<Guid> messageIds, CancellationToken cancellationToken)
        => services.SendAsync(new ForwardChatMessages.Command(chatId, sourceChatId, messageIds));

    /// <summary>Edits a message.</summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="messageId">The message.</param>
    /// <param name="body">The text.</param>
    /// <param name="cancellationToken">Ignored.</param>
    /// <returns>The message as the members see it.</returns>
    public Task<Result<ChatMessageDto>> EditAsync(Guid chatId, Guid messageId, string? body, CancellationToken cancellationToken)
        => services.SendAsync(new EditChatMessage.Command(chatId, messageId, body));

    /// <summary>Deletes a message for everyone.</summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="messageId">The message.</param>
    /// <param name="cancellationToken">Ignored.</param>
    /// <returns>The result.</returns>
    public Task<Result> DeleteAsync(Guid chatId, Guid messageId, CancellationToken cancellationToken) => services.SendAsync(new DeleteChatMessage.Command(chatId, messageId));

    /// <summary>Hides a message for the user.</summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="messageId">The message.</param>
    /// <param name="cancellationToken">Ignored.</param>
    /// <returns>The placeholder.</returns>
    public Task<Result<ChatMessageDto>> HideAsync(Guid chatId, Guid messageId, CancellationToken cancellationToken) => services.SendAsync(new HideChatMessage.Command(chatId, messageId));

    /// <summary>Reacts to a message.</summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="messageId">The message.</param>
    /// <param name="kind">The reaction.</param>
    /// <param name="cancellationToken">Ignored.</param>
    /// <returns>The message as the members see it.</returns>
    public Task<Result<ChatMessageDto>> ReactAsync(Guid chatId, Guid messageId, ReactionKind kind, CancellationToken cancellationToken)
        => services.SendAsync(new ReactToChatMessage.Command(chatId, messageId, kind));

    /// <summary>Marks the chat read.</summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="sequence">The last read number.</param>
    /// <param name="cancellationToken">Ignored.</param>
    /// <returns>The result.</returns>
    public Task<Result> MarkReadAsync(Guid chatId, long sequence, CancellationToken cancellationToken) => services.SendAsync(new MarkChatRead.Command(chatId, sequence));

    /// <summary>Lists the members when the user is one of them.</summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="cancellationToken">Ignored.</param>
    /// <returns>The members, or <see langword="null"/>.</returns>
    public Task<IReadOnlyList<Guid>?> GetMemberIdsAsync(Guid chatId, CancellationToken cancellationToken) => services.SendAsync(new GetChatMemberIds.Query(chatId));

    /// <summary>Opens a file of a message.</summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="attachmentId">The file.</param>
    /// <param name="thumbnail">Whether the thumbnail is wanted.</param>
    /// <param name="cancellationToken">Ignored.</param>
    /// <returns>The stream to download.</returns>
    public Task<Result<DownloadableFile>> OpenAttachmentAsync(Guid chatId, Guid attachmentId, bool thumbnail, CancellationToken cancellationToken)
        => services.SendAsync(new DownloadChatAttachment.Query(chatId, attachmentId, thumbnail));
}
