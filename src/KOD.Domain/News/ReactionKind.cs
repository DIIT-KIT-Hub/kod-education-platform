namespace KOD.Domain.News;

/// <summary>
/// An emotion a reader can leave on an article or a comment (one per reader and target).
/// </summary>
public enum ReactionKind
{
    /// <summary>Like (👍).</summary>
    Like,

    /// <summary>Love (❤️).</summary>
    Love,

    /// <summary>Celebrate (🎉).</summary>
    Celebrate,

    /// <summary>Laugh (😄).</summary>
    Laugh,

    /// <summary>Surprise (😮).</summary>
    Wow,

    /// <summary>Sadness (😢).</summary>
    Sad,
}
