using KOD.Application.Result;

namespace KOD.Application.Abstractions.Services.Emails;

/// <summary>
/// Defines methods for sending emails.
/// </summary>
public interface IEmailService
{
    #region Public methods

    /// <summary>
    /// Sends an email to the specified recipient with the given text content.
    /// </summary>
    /// <param name="to">The recipient email address.</param>
    /// <param name="text">The text content of the email.</param>
    /// <returns>An <see cref="ApiResult{T}"/> indicating success or failure of the email sending operation.</returns>
    Task<ApiResult<bool>> SendEmailAsync(string to, string text);

    #endregion
}
