using Microsoft.AspNetCore.Components;

namespace KOD.Web.DesignSystem.Components;

/// <summary>
/// A round avatar with the person's initials.
/// </summary>
public partial class Avatar
{
    /// <summary>
    /// Gets or sets the full name («Прізвище Ім'я По батькові»).
    /// </summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the size.
    /// </summary>
    [Parameter]
    public ControlSize Size { get; set; } = ControlSize.Medium;

    /// <summary>
    /// Gets the initials: first letters of the last and first names.
    /// </summary>
    private string Initials
    {
        get
        {
            var parts = (Name ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length switch
            {
                0 => "?",
                1 => parts[0][..1],
                _ => string.Concat(parts[0][..1], parts[1][..1]),
            };
        }
    }

    /// <summary>
    /// Gets the CSS classes of the root element.
    /// </summary>
    private string CssClass => Size switch
    {
        ControlSize.Small => "ds-avatar ds-avatar--sm",
        ControlSize.Large => "ds-avatar ds-avatar--lg",
        _ => "ds-avatar",
    };
}
