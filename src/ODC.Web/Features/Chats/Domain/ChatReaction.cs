using ODC.Web.Shared.Domain.Reactions;

namespace ODC.Web.Features.Chats.Domain;

/// <summary>
/// The emotion a member shows on a message (one per member and message, the same set as under news).
/// </summary>
public sealed class ChatReaction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatReaction"/> class.
    /// </summary>
    /// <param name="messageId">The message.</param>
    /// <param name="userId">The member.</param>
    /// <param name="kind">The emotion.</param>
    /// <param name="moment">The current moment.</param>
    public ChatReaction(Guid messageId, Guid userId, ReactionKind kind, DateTimeOffset moment)
    {
        MessageId = messageId;
        UserId = userId;
        Kind = kind;
        CreatedAt = moment;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatReaction"/> class for EF Core materialization.
    /// </summary>
    private ChatReaction()
    {
    }

    /// <summary>
    /// Gets the message.
    /// </summary>
    public Guid MessageId { get; private set; }

    /// <summary>
    /// Gets the member.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the emotion.
    /// </summary>
    public ReactionKind Kind { get; private set; }

    /// <summary>
    /// Gets the moment of the reaction.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Changes the emotion.
    /// </summary>
    /// <param name="kind">The new emotion.</param>
    /// <param name="moment">The current moment.</param>
    public void Change(ReactionKind kind, DateTimeOffset moment)
    {
        Kind = kind;
        CreatedAt = moment;
    }
}
