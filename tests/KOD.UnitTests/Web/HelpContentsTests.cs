using KOD.Domain.Access;
using KOD.Web.Components.Pages.Help;

namespace KOD.UnitTests.Web;

public sealed class HelpContentsTests
{
    [Fact]
    public void Every_chapter_and_topic_has_a_unique_anchor()
    {
        var anchors = HelpContents.Chapters.Select(static c => c.Id)
            .Concat(HelpContents.Chapters.SelectMany(static c => c.Topics).Select(static t => t.Id))
            .ToList();

        Assert.Equal(anchors.Count, anchors.Distinct(StringComparer.Ordinal).Count());
        Assert.All(HelpContents.Chapters, static c => Assert.NotEmpty(c.Topics));
    }

    [Fact]
    public void Access_badges_refer_to_known_permissions()
    {
        var used = HelpContents.Chapters.SelectMany(static c => c.Permissions)
            .Concat(HelpContents.Chapters.SelectMany(static c => c.Topics).SelectMany(static t => t.Permissions));

        Assert.All(used, static code => Assert.True(Permissions.Codes.Contains(code), code));
    }

    [Fact]
    public void Chapters_are_numbered_in_reading_order()
    {
        Assert.Equal(1, HelpContents.NumberOf("intro"));
        Assert.Equal(HelpContents.Chapters.Count, HelpContents.NumberOf(HelpContents.Chapters[^1].Id));
        Assert.Equal("Перший вхід у систему", HelpContents.Topic("account-first-login").Title);
    }
}
