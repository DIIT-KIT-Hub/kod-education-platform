using Microsoft.AspNetCore.Components;

namespace KOD.Web.DesignSystem.Components;

/// <summary>
/// Shows a one-time code input as separate digit cells:
/// <c>&lt;OtpInput AutoSubmit&gt;&lt;InputText class="ds-otp__input" autocomplete="one-time-code" … /&gt;&lt;/OtpInput&gt;</c>.
/// The input keeps its name and bindings; the script keeps only digits in it and mirrors them into the cells.
/// </summary>
public partial class OtpInput
{
    /// <summary>
    /// Gets or sets the number of digits.
    /// </summary>
    [Parameter]
    public int Length { get; set; } = 6;

    /// <summary>
    /// Gets or sets a value indicating whether the enclosing form submits once every digit is entered.
    /// </summary>
    [Parameter]
    public bool AutoSubmit { get; set; }

    /// <summary>
    /// Gets or sets additional CSS classes of the root element.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// Gets or sets the code input.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
