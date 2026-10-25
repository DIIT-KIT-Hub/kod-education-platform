using System.Diagnostics;
using Microsoft.AspNetCore.Components;

namespace KOD.Web.Components.Pages;

/// <summary>
/// The generic error page (production); shows the request id for support.
/// </summary>
public partial class Error
{
    /// <summary>
    /// Gets or sets the HTTP context.
    /// </summary>
    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    /// <summary>
    /// Gets or sets the request identifier.
    /// </summary>
    private string? RequestId { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized() => RequestId = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;
}
