namespace KOD.Application.Abstractions;

/// <summary>
/// Cleans HTML from the rich text editor before it is stored: only the editor's markup survives (paragraphs, headings,
/// lists, quotes, tables, links, images and galleries of uploaded images); scripts, event handlers, styles other than
/// text alignment and images from other sites are removed.
/// </summary>
public interface IRichTextSanitizer
{
    /// <summary>
    /// Sanitizes an HTML fragment.
    /// </summary>
    /// <param name="html">The untrusted HTML.</param>
    /// <returns>The safe HTML.</returns>
    string Sanitize(string html);

    /// <summary>
    /// Determines whether an HTML fragment has no visible content (text, images or rules).
    /// </summary>
    /// <param name="html">The sanitized HTML.</param>
    /// <returns><see langword="true"/> when empty.</returns>
    bool IsEmpty(string html);
}
