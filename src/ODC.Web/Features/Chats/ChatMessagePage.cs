namespace ODC.Web.Features.Chats;

/// <summary>
/// A run of consecutive messages of a chat, oldest first.
/// </summary>
/// <param name="Messages">The messages.</param>
/// <param name="HasMore">Whether more messages exist in the requested direction (older, or newer after a number).</param>
/// <param name="LastSequence">The number of the newest message of the chat.</param>
public sealed record ChatMessagePage(IReadOnlyList<ChatMessageDto> Messages, bool HasMore, long LastSequence);
