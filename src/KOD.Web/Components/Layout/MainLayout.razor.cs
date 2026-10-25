using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace KOD.Web.Components.Layout;

/// <summary>
/// The application shell: sidebar on desktops, top bar with a drawer on tablets and a bottom tab bar on phones.
/// </summary>
public partial class MainLayout
{
    /// <summary>
    /// Gets or sets the authentication state.
    /// </summary>
    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }

    /// <summary>
    /// Gets or sets the navigation entries visible to the user.
    /// </summary>
    private IReadOnlyList<NavigationItem> Items { get; set; } = [];

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (AuthenticationState is not null)
        {
            var state = await AuthenticationState;
            Items = [.. NavigationItem.VisibleTo(state.User)];
        }
    }
}
