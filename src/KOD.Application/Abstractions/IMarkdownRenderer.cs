namespace KOD.Application.Abstractions;

/// <summary>
/// Converts user-authored Markdown into safe HTML.
/// </summary>
public interface IMarkdownRenderer
{
    /// <summary>
    /// Renders Markdown to HTML. Raw HTML in the source is escaped, links get <c>rel="noopener"</c>.
    /// </summary>
    /// <param name="markdown">The Markdown source.</param>
    /// <returns>The HTML.</returns>
    string ToHtml(string markdown);
}
