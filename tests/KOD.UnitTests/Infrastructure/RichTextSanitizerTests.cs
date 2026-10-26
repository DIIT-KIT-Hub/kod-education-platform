using KOD.Application.Common.Media;
using KOD.Infrastructure.Text;

namespace KOD.UnitTests.Infrastructure;

public sealed class RichTextSanitizerTests
{
    /// <summary>
    /// The sanitizer under test.
    /// </summary>
    private readonly RichTextSanitizer _sanitizer = new();

    [Fact]
    public void Keeps_the_markup_of_the_editor()
    {
        var image = MediaUrls.Of(Guid.CreateVersion7());
        var html = $"<h2 style=\"text-align: center\">Заголовок</h2><p><strong>Жирний</strong> <a href=\"https://example.com\">посилання</a></p>"
            + $"<ul><li>Пункт</li></ul><blockquote><p>Цитата</p></blockquote><div data-type=\"gallery\"><img src=\"{image}/thumb\" data-full=\"{image}\"></div>"
            + "<table><tbody><tr><th>А</th><td>Б</td></tr></tbody></table>";

        var clean = _sanitizer.Sanitize(html);

        Assert.Contains("<h2 style=\"text-align: center\">", clean, StringComparison.Ordinal);
        Assert.Contains("<div data-type=\"gallery\">", clean, StringComparison.Ordinal);
        Assert.Contains($"data-full=\"{image}\"", clean, StringComparison.Ordinal);
        Assert.Contains("loading=\"lazy\"", clean, StringComparison.Ordinal);
        Assert.Contains("rel=\"noopener noreferrer nofollow\"", clean, StringComparison.Ordinal);
        Assert.Contains("<th>А</th>", clean, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("<script>alert(1)</script><p>Текст</p>", "script")]
    [InlineData("<p onclick=\"alert(1)\">Текст</p>", "onclick")]
    [InlineData("<a href=\"javascript:alert(1)\">x</a>", "javascript")]
    [InlineData("<img src=\"https://evil.example/a.png\"><p>Текст</p>", "evil")]
    [InlineData("<img src=\"/media/0123/../../secret\"><p>Текст</p>", "secret")]
    [InlineData("<p style=\"background:url(https://evil.example)\">Текст</p>", "evil")]
    [InlineData("<div class=\"x\"><p>Текст</p></div>", "div")]
    [InlineData("<iframe src=\"https://example.com\"></iframe><p>Текст</p>", "iframe")]
    public void Removes_everything_else(string html, string forbidden)
    {
        var clean = _sanitizer.Sanitize(html);

        Assert.DoesNotContain(forbidden, clean, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("<p></p><p> &nbsp;</p>", true)]
    [InlineData("<p>Текст</p>", false)]
    [InlineData("<hr>", false)]
    public void Detects_empty_text(string html, bool empty)
    {
        Assert.Equal(empty, _sanitizer.IsEmpty(_sanitizer.Sanitize(html)));
    }

    [Fact]
    public void Finds_uploaded_images_in_html()
    {
        Guid first = Guid.CreateVersion7(), second = Guid.CreateVersion7();
        var html = $"<img src=\"{MediaUrls.ThumbnailOf(first)}\" data-full=\"{MediaUrls.Of(first)}\"><img src=\"{MediaUrls.Of(second)}\">";

        Assert.True(MediaUrls.IdsIn(html).SetEquals([first, second]));
        Assert.True(MediaUrls.TryParse(MediaUrls.ThumbnailOf(second), out var parsed));
        Assert.Equal(second, parsed);
        Assert.False(MediaUrls.TryParse("/media/not-an-id", out _));
    }
}
