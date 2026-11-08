using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Chats.Domain;

/// <summary>
/// Somebody wrote (or forwarded) messages to a chat.
/// </summary>
/// <param name="ChatId">The chat.</param>
/// <param name="AuthorId">The writer.</param>
/// <param name="LastMessageId">The newest of the new messages.</param>
public sealed record ChatMessagesPosted(Guid ChatId, Guid AuthorId, Guid LastMessageId) : IDomainEvent;
