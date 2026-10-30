using KOD.Application.Features.Profile;
using KOD.Domain.Localization;
using KOD.Domain.Users;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace KOD.Web.Components.Pages.Profile;

/// <summary>
/// Security settings of the current user: password, two-factor authentication, recovery codes and the sign-ins on
/// other devices.
/// </summary>
public partial class ProfileSecurity
{
    /// <summary>
    /// Gets or sets the HTTP context (the page is static: its forms can replace the token cookies).
    /// </summary>
    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    /// <summary>
    /// Gets or sets the profile service.
    /// </summary>
    [Inject]
    private IProfileService ProfileService { get; set; } = default!;

    /// <summary>
    /// Gets or sets the sign-in manager.
    /// </summary>
    [Inject]
    private SignInManager<User> SignInManager { get; set; } = default!;

    /// <summary>
    /// Gets or sets the user manager.
    /// </summary>
    [Inject]
    private UserManager<User> UserManager { get; set; } = default!;

    /// <summary>
    /// Gets or sets the navigation manager.
    /// </summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// Gets or sets the profile.
    /// </summary>
    private ProfileDto? Profile { get; set; }

    /// <summary>
    /// Gets or sets the newly generated recovery codes (shown once).
    /// </summary>
    private IReadOnlyList<string>? Codes { get; set; }

    /// <summary>
    /// Gets or sets the authenticator code that confirms the regeneration.
    /// </summary>
    [SupplyParameterFromForm(FormName = "regenerate-codes")]
    private string? StepUpCode { get; set; }

    /// <summary>
    /// Gets or sets the error of the last action.
    /// </summary>
    private string? ErrorMessage { get; set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync() => await LoadAsync();

    /// <summary>
    /// Generates new recovery codes after the authenticator code is confirmed.
    /// </summary>
    /// <returns>A task that completes when generated.</returns>
    private async Task RegenerateAsync()
    {
        var result = await ProfileService.RegenerateRecoveryCodesAsync(StepUpCode ?? string.Empty, CancellationToken.None);
        Codes = result.IsSuccess ? result.Value : null;
        ErrorMessage = result.IsSuccess ? null : result.Error.Message;
        await LoadAsync();
    }

    /// <summary>
    /// Ends the sign-ins on other devices and re-issues the tokens of this browser with the new security stamp.
    /// </summary>
    /// <returns>A task that completes when done.</returns>
    private async Task SignOutEverywhereAsync()
    {
        var result = await ProfileService.SignOutEverywhereAsync(CancellationToken.None);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        if (await UserManager.GetUserAsync(HttpContext.User) is { } user)
        {
            await SignInManager.RefreshSignInAsync(user);
        }

        Navigation.NavigateTo($"/profile/security?toast={Uri.EscapeDataString(Texts.T("Входи на інших пристроях завершено"))}");
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
