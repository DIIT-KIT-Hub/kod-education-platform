using KOD.Domain.News;

namespace KOD.Application.Features.News.Discussion;

/// <summary>
/// The reactions to an article or a comment.
/// </summary>
/// <param name="Counts">The number of each emotion, most frequent first (emotions nobody chose are absent).</param>
/// <param name="Mine">The current user's emotion, if any.</param>
public sealed record ReactionSummary(IReadOnlyList<ReactionCount> Counts, ReactionKind? Mine)
{
    /// <summary>
    /// Gets a summary without reactions.
    /// </summary>
    public static ReactionSummary Empty { get; } = new([], null);

    /// <summary>
    /// Gets the number of all reactions.
    /// </summary>
    public int Total => Counts.Sum(static c => c.Count);

    /// <summary>
    /// Builds a summary from grouped counts.
    /// </summary>
    /// <param name="counts">The counts of each emotion.</param>
    /// <param name="mine">The current user's emotion.</param>
    /// <returns>The summary.</returns>
    public static ReactionSummary From(IEnumerable<ReactionCount> counts, ReactionKind? mine)
        => new([.. counts.Where(static c => c.Count > 0).OrderByDescending(static c => c.Count).ThenBy(static c => c.Kind)], mine);
}
