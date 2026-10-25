using System.ComponentModel.DataAnnotations;
using KOD.Application.Features.Profile;
using Microsoft.AspNetCore.Components;

namespace KOD.Web.Components.Pages.Profile;

/// <summary>
/// The profile overview: personal data, positions, modules and permissions.
/// </summary>
public partial class ProfileOverview
{
    /// <summary>
    /// Gets or sets the contact form model.
    /// </summary>
    [SupplyParameterFromForm(FormName = "profile-contact")]
    private ContactModel Contact { get; set; } = default!;

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
    /// Gets or sets a value indicating whether the contact form was saved.
    /// </summary>
    private bool Saved { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    private string? ErrorMessage { get; set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var result = await ProfileService.GetAsync(CancellationToken.None);
        Profile = result.IsSuccess ? result.Value : null;
        Contact ??= new ContactModel { PhoneNumber = Profile?.PhoneNumber };
    }

    /// <summary>
    /// Saves the phone number.
    /// </summary>
    /// <returns>A task that completes when saved.</returns>
    private async Task SaveAsync()
    {
        var result = await ProfileService.UpdatePhoneAsync(Contact.PhoneNumber, CancellationToken.None);
        Saved = result.IsSuccess;
        ErrorMessage = result.IsSuccess ? null : result.Error.Message;
    }

    /// <summary>
    /// The contact form.
    /// </summary>
    private sealed class ContactModel
    {
        /// <summary>
        /// Gets or sets the phone number.
        /// </summary>
        [Phone(ErrorMessage = "Некоректний номер телефону.")]
        [StringLength(32)]
        public string? PhoneNumber { get; set; }
    }
}
