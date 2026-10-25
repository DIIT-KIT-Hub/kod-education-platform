using KOD.Application.Common.Access;
using KOD.Application.Common.Paging;
using KOD.Application.Common.Text;
using KOD.Infrastructure.Text;

namespace KOD.UnitTests.Application;

public sealed class CommonTests
{
    [Theory]
    [InlineData("Запущено Кабінет організаційних документів", "zapushcheno-kabinet-orhanizatsiinykh-dokumentiv")]
    [InlineData("Їжак і Юлія", "yizhak-i-yuliia")]
    [InlineData("Наукова конференція «Цифрова трансформація»", "naukova-konferentsiia-tsyfrova-transformatsiia")]
    [InlineData("!!!", "item")]
    public void Slug_uses_the_official_transliteration(string title, string expected)
        => Assert.Equal(expected, Slug.From(title));

    [Fact]
    public void Descendants_include_every_level()
    {
        Guid root = Guid.NewGuid(), department = Guid.NewGuid(), division = Guid.NewGuid(), other = Guid.NewGuid();
        (Guid, Guid?)[] links = [(root, null), (department, root), (division, department), (other, null)];

        var scope = OrganizationTree.WithDescendants(links, [department]);

        Assert.Equivalent(new[] { department, division }, scope, strict: true);
    }

    [Fact]
    public void Path_goes_from_the_root()
    {
        Guid root = Guid.NewGuid(), department = Guid.NewGuid(), division = Guid.NewGuid();
        var parents = new Dictionary<Guid, Guid?> { [root] = null, [department] = root, [division] = department };

        Assert.Equal([root, department, division], OrganizationTree.PathTo(parents, division));
    }

    [Fact]
    public void Organization_of_a_unit_is_the_nearest_organization_above_it()
    {
        Guid holding = Guid.NewGuid(), subsidiary = Guid.NewGuid(), division = Guid.NewGuid(), office = Guid.NewGuid();
        var parents = new Dictionary<Guid, Guid?> { [holding] = null, [office] = holding, [subsidiary] = holding, [division] = subsidiary };
        var organizations = new HashSet<Guid> { holding, subsidiary };

        Assert.Equal(subsidiary, OrganizationTree.OrganizationOf(parents, organizations, division));
        Assert.Equal(subsidiary, OrganizationTree.OrganizationOf(parents, organizations, subsidiary));
        Assert.Equal(holding, OrganizationTree.OrganizationOf(parents, organizations, office));
    }

    [Fact]
    public void Organization_falls_back_to_the_top_level_unit()
    {
        Guid top = Guid.NewGuid(), division = Guid.NewGuid();
        var parents = new Dictionary<Guid, Guid?> { [top] = null, [division] = top };

        Assert.Equal(top, OrganizationTree.OrganizationOf(parents, new HashSet<Guid>(), division));
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

        var html = renderer.ToHtml("<script>alert(1)</script>\n\n[x](javascript:alert(1)) [ok](https://example.com)");

        Assert.DoesNotContain("<script>", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("href=\"https://example.com\"", html, StringComparison.Ordinal);
        Assert.Contains("noopener", html, StringComparison.Ordinal);
    }
}
