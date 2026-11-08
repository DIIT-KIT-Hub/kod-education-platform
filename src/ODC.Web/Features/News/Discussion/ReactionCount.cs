using ODC.Web.Features.News.Domain;
using ODC.Web.Shared.Domain.Reactions;

namespace ODC.Web.Features.News.Discussion;

/// <summary>
/// How many readers chose an emotion.
/// </summary>
/// <param name="Kind">The emotion.</param>
/// <param name="Count">The number of readers.</param>
public sealed record ReactionCount(ReactionKind Kind, int Count);
