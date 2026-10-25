using KOD.Application.Common.Access;
using KOD.Application.Common.Paging;
using KOD.Application.Common.Text;
using KOD.Infrastructure.Text;

namespace KOD.UnitTests.Application;

public sealed class CommonTests
{
    [Theory]
    [InlineData("Запущено Кабінет освітніх даних", "zapushcheno-kabinet-osvitnikh-danykh")]
    [InlineData("Їжак і Юлія", "yizhak-i-yuliia")]
    [InlineData("Наукова конференція «Цифрова трансформація»", "naukova-konferentsiia-tsyfrova-transformatsiia")]
    [InlineData("!!!", "item")]
    public void Slug_uses_the_official_transliteration(string title, string expected)
        => Assert.Equal(expected, Slug.From(title));

    [Fact]
    public void Descendants_include_every_level()
    {
        Guid root = Guid.NewGuid(), faculty = Guid.NewGuid(), department = Guid.NewGuid(), other = Guid.NewGuid();
        (Guid, Guid?)[] links = [(root, null), (faculty, root), (department, faculty), (other, null)];

        var scope = OrganizationTree.WithDescendants(links, [faculty]);

        Assert.Equal([faculty, department], scope.Order());
    }

    [Fact]
    public void Path_goes_from_the_root()
    {
        Guid root = Guid.NewGuid(), faculty = Guid.NewGuid(), department = Guid.NewGuid();
        var parents = new Dictionary<Guid, Guid?> { [root] = null, [faculty] = root, [department] = faculty };

        Assert.Equal([root, faculty, department], OrganizationTree.PathTo(parents, department));
    }

    [Fact]
    public void Page_request_is_clamped()
    {
        var request = new PageRequest { Page = -5, PageSize = 10_000, Search = "   " };

        Assert.Equal(1, request.Page);
        Assert.Equal(PageRequest.MaxPageSize, request.PageSize);
        Assert.Null(request.Search);
    }

    [Fact]
    public void Paged_result_reports_the_window()
    {
        var page = new PagedResult<int>([21, 22], 2, 20, 22);

        Assert.Equal(2, page.TotalPages);
        Assert.Equal(21, page.FirstItemIndex);
        Assert.Equal(22, page.LastItemIndex);
        Assert.True(page.HasPrevious);
        Assert.False(page.HasNext);
    }

    [Fact]
    public void Markdown_never_outputs_raw_html_or_script_urls()
    {
        var renderer = new MarkdigRenderer();

        var html = renderer.ToHtml("<script>alert(1)</script>\n\n[x](javascript:alert(1)) [ok](https://university.edu.ua)");

        Assert.DoesNotContain("<script>", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("href=\"https://university.edu.ua\"", html, StringComparison.Ordinal);
        Assert.Contains("noopener", html, StringComparison.Ordinal);
    }
}
