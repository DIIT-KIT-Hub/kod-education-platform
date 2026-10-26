using Microsoft.AspNetCore.Components;

namespace KOD.Web.DesignSystem.Components;

/// <summary>
/// A person in a list, a table cell or a picker: the profile photo, the name (optionally a link) and a line of details.
/// </summary>
public partial class PersonName
{
    /// <summary>
    /// Gets or sets the full name.
    /// </summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the profile photo.
    /// </summary>
    [Parameter]
    public Guid? PhotoId { get; set; }

    /// <summary>
    /// Gets or sets the details under the name (position, e-mail).
    /// </summary>
    [Parameter]
    public string? Meta { get; set; }

    /// <summary>
    /// Gets or sets the link of the name.
    /// </summary>
    [Parameter]
    public string? Href { get; set; }

    /// <summary>
    /// Gets or sets the avatar size.
    /// </summary>
    [Parameter]
    public ControlSize Size { get; set; } = ControlSize.Small;
}
