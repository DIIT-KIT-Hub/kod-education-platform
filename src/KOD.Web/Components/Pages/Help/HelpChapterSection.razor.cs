using Microsoft.AspNetCore.Components;

namespace KOD.Web.Components.Pages.Help;

/// <summary>
/// Renders a chapter of the manual. The title, icon and access badge come from <see cref="HelpContents"/>, the same
/// catalog that builds the table of contents, so headings and contents cannot drift apart.
/// </summary>
public partial class HelpChapterSection
{
    /// <summary>
    /// Gets or sets the chapter anchor.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the chapter text.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets the chapter.
    /// </summary>
    private HelpChapter Chapter => HelpContents.Chapter(Id);

    /// <summary>
    /// Gets the chapter number.
    /// </summary>
    private int Number => HelpContents.NumberOf(Id);
}
