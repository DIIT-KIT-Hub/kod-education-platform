namespace KOD.Web.Components.Pages.Help;

/// <summary>
/// A chapter of the user manual: one module of the cabinet.
/// </summary>
/// <param name="Id">The anchor (unique on the page).</param>
/// <param name="Title">The title.</param>
/// <param name="Icon">The design system icon.</param>
/// <param name="Summary">One sentence about the module.</param>
/// <param name="Permissions">Permissions of which at least one opens the module; empty for everyone.</param>
/// <param name="Topics">The topics in reading order.</param>
public sealed record HelpChapter(string Id, string Title, string Icon, string Summary, IReadOnlyList<string> Permissions, IReadOnlyList<HelpTopic> Topics);
