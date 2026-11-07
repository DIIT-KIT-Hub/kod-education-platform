using ODC.Application.Common.Files;
using ODC.Application.Common.Paging;
using ODC.Application.Common.Results;

namespace ODC.Application.Features.Users;

/// <summary>
/// User account administration use cases. There is no self-registration: accounts are created here or imported, without
/// a password; the owner activates the account from the sign-in page.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Lists user accounts.
    /// </summary>
    /// <param name="query">The paging and filter parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The page of users.</returns>
    Task<Result<PagedResult<UserListItem>>> ListAsync(UserQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a user account.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user.</returns>
    Task<Result<UserDetails>> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Creates an account without a password and sends the owner the invitation to activate it.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The identifier of the new user.</returns>
    Task<Result<Guid>> CreateAsync(SaveUserCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an account.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> UpdateAsync(Guid id, SaveUserCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Blocks or unblocks an account. Blocking ends all sessions.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="isActive">The new state.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken);

    /// <summary>
    /// Removes the password and ends all sessions: the owner gets an e-mail and chooses a new password through the
    /// activation.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> ResetPasswordAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Sends the invitation to activate an account again.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> SendInvitationAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Resets the authenticator (the user re-enrolls at the next sign-in) and ends all sessions.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> ResetAuthenticatorAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Ends all sessions of a user and removes a lockout.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    Task<Result> RevokeSessionsAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Imports users from a spreadsheet. Nothing is saved when any row is invalid or when <paramref name="dryRun"/> is set.
    /// </summary>
    /// <param name="file">The spreadsheet.</param>
    /// <param name="dryRun">Whether to only validate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The import report.</returns>
    Task<Result<UserImportReport>> ImportAsync(FileUpload file, bool dryRun, CancellationToken cancellationToken);

    /// <summary>
    /// Exports users matching the filter.
    /// </summary>
    /// <param name="query">The filter (paging is ignored).</param>
    /// <param name="format">The file format.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The file.</returns>
    Task<Result<DownloadableFile>> ExportAsync(UserQuery query, SpreadsheetFormat format, CancellationToken cancellationToken);

    /// <summary>
    /// Returns an empty import template with sample rows.
    /// </summary>
    /// <param name="format">The file format.</param>
    /// <returns>The file.</returns>
    Result<DownloadableFile> GetImportTemplate(SpreadsheetFormat format);
}
