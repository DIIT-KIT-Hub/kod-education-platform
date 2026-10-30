using KOD.Application.Abstractions;
using KOD.Application.Common.Files;
using KOD.Application.Common.Results;

namespace KOD.Application.Features.Profile;

/// <summary>
/// Self-service use cases of the signed-in user.
/// </summary>
public interface IProfileService
{
    /// <summary>
    /// Gets the current user's profile.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The profile.</returns>
    Task<Result<ProfileDto>> GetAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Updates the contact phone number.
    /// </summary>
    /// <param name="phoneNumber">The phone number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> UpdatePhoneAsync(string? phoneNumber, CancellationToken cancellationToken);

    /// <summary>
    /// Saves the interface language of the current user.
    /// </summary>
    /// <param name="language">A supported language code (<see cref="KOD.Domain.Localization.Languages"/>).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> SetLanguageAsync(string language, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces the profile photo: the chosen square is optimized to AVIF and the previous photo is deleted.
    /// </summary>
    /// <param name="file">The uploaded photo.</param>
    /// <param name="crop">The square chosen by the user (the centre when <see langword="null"/>).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The identifier of the new photo.</returns>
    Task<Result<Guid>> SetPhotoAsync(FileUpload file, ImageCrop? crop, CancellationToken cancellationToken);

    /// <summary>
    /// Changes the password and clears the "must change password" flag.
    /// </summary>
    /// <param name="currentPassword">The current password.</param>
    /// <param name="newPassword">The new password.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken);

    /// <summary>
    /// Generates a new set of recovery codes after confirming a code of the authenticator app (step-up authentication).
    /// </summary>
    /// <param name="authenticatorCode">The current authenticator code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The codes (shown once).</returns>
    Task<Result<IReadOnlyList<string>>> RegenerateRecoveryCodesAsync(string authenticatorCode, CancellationToken cancellationToken);

    /// <summary>
    /// Ends every sign-in of the current user: the security stamp changes, so tokens issued before stop being renewed
    /// (at most one access token lifetime later). The caller re-issues the tokens of the current browser to keep it signed in.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> SignOutEverywhereAsync(CancellationToken cancellationToken);
}
