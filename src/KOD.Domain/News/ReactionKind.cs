namespace KOD.Domain.News;

/// <summary>
/// An emotion a reader can leave on an article or a comment (one per reader and target). Stored by name, so the order
/// of the members is the order of the reaction menu and may change.
/// </summary>
public enum ReactionKind
{
    /// <summary>Like (👍).</summary>
    Like,

    /// <summary>Dislike (👎).</summary>
    Dislike,

    /// <summary>Love (❤️).</summary>
    Love,

    /// <summary>Applause (👏).</summary>
    Clap,

    /// <summary>Celebrate (🎉).</summary>
    Celebrate,

    /// <summary>Laugh (😄).</summary>
    Laugh,

    /// <summary>Surprise (😮).</summary>
    Wow,

    /// <summary>Doubt (🤔).</summary>
    Thinking,

    /// <summary>Sadness (😢).</summary>
    Sad,

    /// <summary>Anger (😡).</summary>
    Angry,
}
