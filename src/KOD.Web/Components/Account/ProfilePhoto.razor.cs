using KOD.Web.Security;
using Microsoft.AspNetCore.Components;

namespace KOD.Web.Components.Account;

/// <summary>
/// Uploads the profile photo. Users without a photo are redirected here until they upload one
/// (<see cref="AccountSetupMiddleware"/>); the form posts to <c>/account/photo</c>.
/// </summary>
public partial class ProfilePhoto
{
    /// <summary>
    /// Gets or sets the address to return to.
    /// </summary>
    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    /// <summary>
    /// Gets or sets the error of the previous attempt.
    /// </summary>
    [SupplyParameterFromQuery]
    private string? Error { get; set; }

    /// <summary>
    /// Gets or sets the HTTP context.
    /// </summary>
    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    /// <summary>
    /// Gets a value indicating whether the user has no photo yet and cannot use the cabinet without one.
    /// </summary>
    private bool IsRequired => HttpContext.User.PhotoId is null;
}
