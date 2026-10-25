namespace KOD.Web.Components.Pages.Help;

/// <summary>
/// A topic of the user manual (a subsection with its own anchor).
/// </summary>
/// <param name="Id">The anchor (unique on the page).</param>
/// <param name="Title">The title shown in the heading and in the table of contents.</param>
/// <param name="Permissions">Permissions of which at least one is needed to use the described feature; empty for everyone.</param>
public sealed record HelpTopic(string Id, string Title, IReadOnlyList<string> Permissions)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HelpTopic"/> class for a feature available to everyone.
    /// </summary>
    /// <param name="id">The anchor.</param>
    /// <param name="title">The title.</param>
    public HelpTopic(string id, string title)
        : this(id, title, [])
    {
    }
}
