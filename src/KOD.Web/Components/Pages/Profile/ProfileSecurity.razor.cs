using KOD.Application.Features.Profile;
using Microsoft.AspNetCore.Components;

namespace KOD.Web.Components.Pages.Profile;

/// <summary>
/// Security settings of the current user: password, two-factor authentication and recovery codes.
/// </summary>
public partial class ProfileSecurity
{
    /// <summary>
    /// Gets or sets the profile service.
    /// </summary>
    [Inject]
    private IProfileService ProfileService { get; set; } = default!;

    /// <summary>
    /// Gets or sets the profile.
    /// </summary>
    private ProfileDto? Profile { get; set; }

    /// <summary>
    /// Gets or sets the newly generated recovery codes (shown once).
    /// </summary>
    private IReadOnlyList<string>? Codes { get; set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync() => await LoadAsync();

    /// <summary>
    /// Generates new recovery codes.
    /// </summary>
    /// <returns>A task that completes when generated.</returns>
    private async Task RegenerateAsync()
    {
        var result = await ProfileService.RegenerateRecoveryCodesAsync(CancellationToken.None);
        Codes = result.IsSuccess ? result.Value : null;
        await LoadAsync();
    }

    /// <summary>
    /// Loads the profile.
    /// </summary>
    /// <returns>A task that completes when loaded.</returns>
    private async Task LoadAsync()
    {
        var result = await ProfileService.GetAsync(CancellationToken.None);
        Profile = result.IsSuccess ? result.Value : null;
    }
}
