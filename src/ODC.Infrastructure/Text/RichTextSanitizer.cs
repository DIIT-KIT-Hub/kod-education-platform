using System.Net;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Ganss.Xss;
using ODC.Application.Abstractions;
using ODC.Application.Common.Media;

namespace ODC.Infrastructure.Text;

/// <summary>
/// An allow-list sanitizer (HtmlSanitizer) for the markup of the news editor. Images must be uploaded images
/// (<c>/media/{id}</c>), links may use http(s), mailto and tel, external links open in a new tab without a referrer,
/// and only text alignment and table column widths survive in <c>style</c>.
/// </summary>
internal sealed partial class RichTextSanitizer : IRichTextSanitizer
{
    /// <summary>
    /// The configured sanitizer (thread-safe once configured).
    /// </summary>
    private readonly HtmlSanitizer _sanitizer;

    /// <summary>
    /// The sanitizer of chat messages (text formatting only; the chat editor produces nothing else).
    /// </summary>
    private readonly HtmlSanitizer _messageSanitizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="RichTextSanitizer"/> class.
    /// </summary>
    public RichTextSanitizer()
    {
        _sanitizer = new HtmlSanitizer(new HtmlSanitizerOptions
        {
            AllowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "p", "br", "h2", "h3", "h4", "strong", "b", "em", "i", "u", "s", "code", "pre", "blockquote",
                "ul", "ol", "li", "hr", "a", "img", "div", "table", "thead", "tbody", "tr", "th", "td", "colgroup", "col",
            },
            AllowedAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "href", "target", "rel", "title", "src", "alt", "width", "height", "loading", "decoding",
                "data-type", "data-full", "colspan", "rowspan", "colwidth", "start", "style",
            },
            AllowedCssProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "text-align", "width", "min-width" },
            AllowedSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "http", "https", "mailto", "tel" },
            UriAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "href", "src", "data-full" },
        });
        _sanitizer.PostProcessNode += static (_, args) =>
        {
            if (args.Node is IElement element)
            {
                Normalize(element);
            }
        };

        _messageSanitizer = new HtmlSanitizer(new HtmlSanitizerOptions
        {
            AllowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "p", "br", "strong", "b", "em", "i", "u", "s", "code", "pre", "blockquote", "ul", "ol", "li", "a",
            },
            AllowedAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "href", "target", "rel", "start" },
            AllowedSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "http", "https", "mailto", "tel" },
            UriAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "href" },
        });
        _messageSanitizer.PostProcessNode += static (_, args) =>
        {
            if (args.Node is IElement element)
            {
                Normalize(element);
            }
        };
    }

    /// <inheritdoc />
    public string Sanitize(string html) => _sanitizer.Sanitize(html ?? string.Empty).Trim();

    /// <inheritdoc />
    public string SanitizeMessage(string html) => _messageSanitizer.Sanitize(html ?? string.Empty).Trim();

    /// <inheritdoc />
    public string ToText(string html)
    {
        var text = BlockEndPattern().Replace(html ?? string.Empty, "\n");
        text = WebUtility.HtmlDecode(TagPattern().Replace(text, string.Empty)).Replace(' ', ' ');
        var lines = text.Split('\n').Select(static line => SpacePattern().Replace(line, " ").Trim());
        return BlankLinesPattern().Replace(string.Join('\n', lines), "\n\n").Trim();
    }

    /// <inheritdoc />
    public bool IsEmpty(string html)
    {
        if (string.IsNullOrWhiteSpace(html) || ContentElementPattern().IsMatch(html))
        {
            return string.IsNullOrWhiteSpace(html);
        }

        var text = TagPattern().Replace(html, string.Empty).Replace("&nbsp;", " ", StringComparison.Ordinal);
        return string.IsNullOrWhiteSpace(text);
    }

    /// <summary>
    /// Applies the rules the allow-list cannot express.
    /// </summary>
    /// <param name="element">The element.</param>
    private static void Normalize(IElement element)
    {
        switch (element.LocalName)
        {
            case "img":
                if (!MediaUrls.TryParse(element.GetAttribute("src"), out _))
                {
                    element.Remove();
                    return;
                }

                if (element.HasAttribute("data-full") && !MediaUrls.TryParse(element.GetAttribute("data-full"), out _))
                {
                    element.RemoveAttribute("data-full");
                }

                element.SetAttribute("loading", "lazy");
                element.SetAttribute("decoding", "async");
                break;

            case "div":
                // The only block container of the editor is the gallery; anything else is unwrapped.
                if (element.GetAttribute("data-type") != "gallery")
                {
                    element.Replace([.. element.ChildNodes]);
                }

                break;

            case "a":
                var href = element.GetAttribute("href") ?? string.Empty;
                if (href.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || href.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    element.SetAttribute("target", "_blank");
                    element.SetAttribute("rel", "noopener noreferrer nofollow");
                }
                else
                {
                    element.RemoveAttribute("target");
                    element.RemoveAttribute("rel");
                }

                break;
        }

        if (element.LocalName != "div")
        {
            element.RemoveAttribute("data-type");
        }
    }

    /// <summary>
    /// Matches elements that are content without text.
    /// </summary>
    /// <returns>The regular expression.</returns>
    [GeneratedRegex("<(img|hr|table)\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ContentElementPattern();

    /// <summary>
    /// Matches a tag.
    /// </summary>
    /// <returns>The regular expression.</returns>
    [GeneratedRegex("<[^>]*>", RegexOptions.CultureInvariant)]
    private static partial Regex TagPattern();

    /// <summary>
    /// Matches the end of a block (and a line break), which separates lines of the visible text.
    /// </summary>
    /// <returns>The regular expression.</returns>
    [GeneratedRegex("<br\\s*/?>|</(p|li|pre|blockquote|h[1-6]|tr|div)>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BlockEndPattern();

    /// <summary>
    /// Matches a run of spaces and tabs.
    /// </summary>
    /// <returns>The regular expression.</returns>
    [GeneratedRegex("[ \\t\\r]+", RegexOptions.CultureInvariant)]
    private static partial Regex SpacePattern();

    /// <summary>
    /// Matches more than one empty line.
    /// </summary>
    /// <returns>The regular expression.</returns>
    [GeneratedRegex("\\n{3,}", RegexOptions.CultureInvariant)]
    private static partial Regex BlankLinesPattern();
}
