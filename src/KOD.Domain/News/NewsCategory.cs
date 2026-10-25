namespace KOD.Domain.News;

/// <summary>
/// Category of a news article.
/// </summary>
public enum NewsCategory
{
    /// <summary>General university news.</summary>
    News = 0,

    /// <summary>Official announcement.</summary>
    Announcement = 1,

    /// <summary>Upcoming event.</summary>
    Event = 2,

    /// <summary>Education process: schedule, sessions, admission.</summary>
    Education = 3,

    /// <summary>Science and research.</summary>
    Science = 4,
}
