using KOD.Application.Common.Paging;
using KOD.Application.Features.Profile;
using KOD.Domain.Localization;
using Microsoft.AspNetCore.Components;

namespace KOD.Web.Components.Pages.Profile;

/// <summary>
/// Lists the current user's server-side sessions and allows ending them.
/// </summary>
public partial class ProfileSessions
{
    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    [SupplyParameterFromQuery]
    private int? Page { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    [SupplyParameterFromQuery]
    private int? PageSize { get; set; }

    /// <summary>
    /// Gets or sets the profile service.
    /// </summary>
    [Inject]
    private IProfileService ProfileService { get; set; } = default!;

    /// <summary>
    /// Gets or sets the navigation manager.
    /// </summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// Gets or sets all sessions.
    /// </summary>
    private IReadOnlyList<SessionDto> Sessions { get; set; } = [];

    /// <summary>
    /// Gets the page size.
    /// </summary>
    private int Size => Math.Clamp(PageSize ?? PageRequest.DefaultPageSize, 1, PageRequest.MaxPageSize);

    /// <summary>
    /// Gets the current page number.
    /// </summary>
    private int CurrentPage => Math.Clamp(Page ?? 1, 1, Math.Max(1, (int)Math.Ceiling(Sessions.Count / (double)Size)));

    /// <summary>
    /// Gets the sessions of the current page.
    /// </summary>
    private IReadOnlyList<SessionDto> PageItems => [.. Sessions.Skip((CurrentPage - 1) * Size).Take(Size)];

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var result = await ProfileService.GetSessionsAsync(CancellationToken.None);
        Sessions = result.IsSuccess ? result.Value : [];
    }

    /// <summary>
    /// Determines whether the user agent belongs to a phone or tablet.
    /// </summary>
    /// <param name="userAgent">The user agent.</param>
    /// <returns><see langword="true"/> for mobile devices.</returns>
    private static bool IsMobile(string? userAgent)
        => userAgent is not null && (userAgent.Contains("Mobile", StringComparison.Ordinal) || userAgent.Contains("Android", StringComparison.Ordinal));

    /// <summary>
    /// Produces a short browser and OS description from a user agent.
    /// </summary>
    /// <param name="userAgent">The user agent.</param>
    /// <returns>The description.</returns>
    private static string DescribeAgent(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
        {
            return Texts.T("Невідомий пристрій");
        }

        var browser = userAgent switch
        {
            _ when userAgent.Contains("Edg/", StringComparison.Ordinal) => "Microsoft Edge",
            _ when userAgent.Contains("OPR/", StringComparison.Ordinal) => "Opera",
            _ when userAgent.Contains("Firefox/", StringComparison.Ordinal) => "Firefox",
            _ when userAgent.Contains("Chrome/", StringComparison.Ordinal) => "Chrome",
            _ when userAgent.Contains("Safari/", StringComparison.Ordinal) => "Safari",
            _ => Texts.T("Браузер"),
        };
        var os = userAgent switch
        {
            _ when userAgent.Contains("Windows", StringComparison.Ordinal) => "Windows",
            _ when userAgent.Contains("Android", StringComparison.Ordinal) => "Android",
            _ when userAgent.Contains("iPhone", StringComparison.Ordinal) || userAgent.Contains("iPad", StringComparison.Ordinal) => "iOS",
            _ when userAgent.Contains("Mac OS", StringComparison.Ordinal) => "macOS",
            _ when userAgent.Contains("Linux", StringComparison.Ordinal) => "Linux",
            _ => Texts.T("ОС невідома"),
        };
        return $"{browser} · {os}";
    }

    /// <summary>
    /// Ends one session.
    /// </summary>
    /// <param name="id">The session identifier.</param>
    /// <returns>A task that completes when ended.</returns>
    private async Task RevokeAsync(Guid id)
    {
        await ProfileService.RevokeSessionAsync(id, CancellationToken.None);
        Navigation.NavigateTo($"/profile/sessions?toast={Uri.EscapeDataString(Texts.T("Сесію завершено"))}");
    }

    /// <summary>
    /// Ends every other session.
    /// </summary>
    /// <returns>A task that completes when ended.</returns>
    private async Task RevokeOthersAsync()
    {
        var result = await ProfileService.RevokeOtherSessionsAsync(CancellationToken.None);
        var count = result.IsSuccess ? result.Value : 0;
        Navigation.NavigateTo($"/profile/sessions?toast={Uri.EscapeDataString(Texts.T("Завершено сесій: {0}", count))}");
    }
}
