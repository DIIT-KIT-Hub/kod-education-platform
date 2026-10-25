using System.Security.Claims;
using KOD.Application.Features.Dashboard;
using KOD.Web.Components.Shared;
using KOD.Web.Security;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace KOD.Web.Components.Pages;

/// <summary>
/// The home page: greeting, key figures, documents awaiting the user's signature and the latest news.
/// </summary>
public partial class Home
{
    /// <summary>
    /// Gets or sets the authentication state.
    /// </summary>
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    /// <summary>
    /// Gets or sets the dashboard service.
    /// </summary>
    [Inject]
    private IDashboardService Dashboard { get; set; } = default!;

    /// <summary>
    /// Gets or sets the clock.
    /// </summary>
    [Inject]
    private TimeProvider TimeProvider { get; set; } = default!;

    /// <summary>
    /// Gets or sets the current user.
    /// </summary>
    private ClaimsPrincipal User { get; set; } = new();

    /// <summary>
    /// Gets or sets the dashboard data (streamed in after the first paint).
    /// </summary>
    private DashboardSummary? Summary { get; set; }

    /// <summary>
    /// Gets the user's first name.
    /// </summary>
    private string FirstName
    {
        get
        {
            var parts = User.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 1 ? parts[1] : User.FullName;
        }
    }

    /// <summary>
    /// Gets a greeting for the time of day (Kyiv time).
    /// </summary>
    private string Greeting
    {
        get
        {
            var hour = int.Parse(Display.DateTime(TimeProvider.GetUtcNow())[^5..^3], Display.Culture);
            return hour switch
            {
                < 5 => "Доброї ночі,",
                < 12 => "Доброго ранку,",
                < 18 => "Доброго дня,",
                _ => "Доброго вечора,",
            };
        }
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        User = (await AuthenticationState).User;
        Summary = await Dashboard.GetAsync(CancellationToken.None);
    }
}
