using KOD.Application.Common.Files;
using KOD.Application.Common.Paging;
using KOD.Application.Common.Results;

namespace KOD.Application.Features.Users;

/// <summary>
/// User account administration use cases. There is no self-registration: accounts are created here or imported.
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
    /// Creates an account with a temporary password.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The one-time credentials.</returns>
    Task<Result<UserCredentials>> CreateAsync(SaveUserCommand command, CancellationToken cancellationToken);

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
    /// Replaces the password with a temporary one and ends all sessions.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The one-time credentials.</returns>
    Task<Result<UserCredentials>> ResetPasswordAsync(Guid id, CancellationToken cancellationToken);

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

    /// <summary>
    /// Builds a credentials file for distributing temporary passwords.
    /// </summary>
    /// <param name="credentials">The credentials.</param>
    /// <param name="format">The file format.</param>
    /// <returns>The file.</returns>
    DownloadableFile BuildCredentialsFile(IEnumerable<UserCredentials> credentials, SpreadsheetFormat format);
}
