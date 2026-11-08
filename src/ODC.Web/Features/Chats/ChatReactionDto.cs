using ODC.Web.Shared.Domain.Reactions;

namespace ODC.Web.Features.Chats;

/// <summary>
/// The people who showed one emotion on a message.
/// </summary>
/// <param name="Kind">The emotion.</param>
/// <param name="UserIds">The people.</param>
public sealed record ChatReactionDto(ReactionKind Kind, IReadOnlyList<Guid> UserIds);
