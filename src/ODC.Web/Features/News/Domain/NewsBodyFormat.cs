namespace ODC.Web.Features.News.Domain;

/// <summary>
/// The markup of a news article body.
/// </summary>
public enum NewsBodyFormat
{
    /// <summary>Markdown without raw HTML (articles written before the rich text editor).</summary>
    Markdown,

    /// <summary>Sanitized HTML produced by the rich text editor.</summary>
    Html,
}
