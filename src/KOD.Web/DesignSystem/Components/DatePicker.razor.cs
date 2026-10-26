using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace KOD.Web.DesignSystem.Components;

/// <summary>
/// A date field with the design system calendar instead of the browser's date popup. Shows the date in the format of the
/// interface language and submits it as <c>yyyy-MM-dd</c> under <see cref="Name"/>, so it works in static GET/POST forms;
/// in interactive components bind <see cref="Value"/> / <see cref="ValueChanged"/>.
/// </summary>
public partial class DatePicker
{
    /// <summary>
    /// Gets or sets the form field name of the ISO value.
    /// </summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the id of the visible field (for a <c>&lt;label for&gt;</c>).
    /// </summary>
    [Parameter]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the date.
    /// </summary>
    [Parameter]
    public DateOnly? Value { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the user picks or clears a date (interactive render modes only).
    /// </summary>
    [Parameter]
    public EventCallback<DateOnly?> ValueChanged { get; set; }

    /// <summary>
    /// Gets or sets the earliest allowed date.
    /// </summary>
    [Parameter]
    public DateOnly? Min { get; set; }

    /// <summary>
    /// Gets or sets the latest allowed date.
    /// </summary>
    [Parameter]
    public DateOnly? Max { get; set; }

    /// <summary>
    /// Gets or sets the accessible name when there is no visible label.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the placeholder of the empty field (the date format by default).
    /// </summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether choosing a date submits the enclosing form (like <c>data-autosubmit</c>).
    /// </summary>
    [Parameter]
    public bool AutoSubmit { get; set; }

    /// <summary>
    /// Gets or sets additional CSS classes of the root element.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// Gets the date pattern of the interface language (<c>dd.MM.yyyy</c>, <c>dd/MM/yyyy</c>), shared with the script.
    /// </summary>
    private static string Pattern => CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;

    /// <summary>
    /// Gets the date as shown in the field.
    /// </summary>
    private string? DisplayValue => Value?.ToString(Pattern, CultureInfo.CurrentCulture);

    /// <summary>
    /// Formats a date as <c>yyyy-MM-dd</c>.
    /// </summary>
    /// <param name="date">The date.</param>
    /// <returns>The ISO date or <see langword="null"/>.</returns>
    private static string? IsoOf(DateOnly? date) => date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>
    /// Reports a date picked in the calendar or typed into the field.
    /// </summary>
    /// <param name="args">The change of the hidden ISO value.</param>
    /// <returns>A task that completes when the callback ran.</returns>
    private async Task OnChangedAsync(ChangeEventArgs args)
    {
        Value = DateOnly.TryParseExact(args.Value as string, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
        await ValueChanged.InvokeAsync(Value);
    }
}
