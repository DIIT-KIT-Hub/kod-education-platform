namespace KOD.Domain.News;

/// <summary>
/// Category of a news article.
/// </summary>
public enum NewsCategory
{
    /// <summary>General news.</summary>
    News = 0,

    /// <summary>Official announcement.</summary>
    Announcement = 1,

    /// <summary>Upcoming event.</summary>
    Event = 2,

    /// <summary>Training and professional development.</summary>
    Training = 3,

    /// <summary>Projects and achievements.</summary>
    Projects = 4,
}
