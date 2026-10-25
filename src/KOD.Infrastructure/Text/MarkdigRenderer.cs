using KOD.Application.Abstractions;
using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace KOD.Infrastructure.Text;

/// <summary>
/// Renders Markdown with Markdig. Raw HTML is disabled and only http(s), mailto, tel and site-relative URLs are kept,
/// which makes the output safe against script injection.
/// </summary>
internal sealed class MarkdigRenderer : IMarkdownRenderer
{
    /// <summary>
    /// The shared, thread-safe pipeline.
    /// </summary>
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    /// <inheritdoc />
    public string ToHtml(string markdown)
    {
        var document = Markdown.Parse(markdown ?? string.Empty, Pipeline);
        foreach (var link in document.Descendants<LinkInline>())
        {
            if (!IsSafeUrl(link.Url))
            {
                link.Url = "#";
            }

            if (!link.IsImage)
            {
                link.GetAttributes().AddPropertyIfNotExist("rel", "noopener noreferrer nofollow");
            }
        }

        foreach (var autolink in document.Descendants<AutolinkInline>().Where(static a => !IsSafeUrl(a.Url)))
        {
            autolink.Url = "#";
        }

        return document.ToHtml(Pipeline);
    }

    /// <summary>
    /// Allows only harmless URL schemes.
    /// </summary>
    /// <param name="url">The URL.</param>
    /// <returns><see langword="true"/> when the URL is safe.</returns>
    private static bool IsSafeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var value = url.Trim();
        if (value.StartsWith('/') && !value.StartsWith("//", StringComparison.Ordinal))
        {
            return true;
        }

        return value.StartsWith('#')
            || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("tel:", StringComparison.OrdinalIgnoreCase);
    }
}
