using Microsoft.AspNetCore.Components;

namespace KOD.Web.Components.Pages.Help;

/// <summary>
/// Renders a topic of the manual with an anchor link; the title comes from <see cref="HelpContents"/>.
/// </summary>
public partial class HelpTopicSection
{
    /// <summary>
    /// Gets or sets the topic anchor.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the topic text.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets the topic.
    /// </summary>
    private HelpTopic Topic => HelpContents.Topic(Id);
}
