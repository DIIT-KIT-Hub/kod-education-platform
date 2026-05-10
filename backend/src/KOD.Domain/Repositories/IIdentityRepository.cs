using KOD.Domain.Entities.Users;

namespace KOD.Domain.Repositories;

/// <summary>
/// Defines methods for accessing and managing user identity data in persistence storage.
/// </summary>
public interface IIdentityRepository
{
    #region Public methods

    /// <summary>
    /// Retrieves the login details of a user by their email.
    /// </summary>
    /// <param name="email">The email address of the user to look up.</param>
    /// <returns>
    /// A <see cref="UserLoginDetails"/> object containing login information if found; otherwise, <c>null</c>.
    /// </returns>
    Task<UserLoginDetails?> GetUserLoginDetailsByEmailAsync(string email);

    /// <summary>
    /// Retrieves a user entity by their email address.
    /// </summary>
    /// <param name="email">The email address of the user.</param>
    /// <returns>
    /// An <see cref="ApplicationUser"/> if a user with the given email exists; otherwise, <c>null</c>.
    /// </returns>
    Task<ApplicationUser?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Confirms a user's account by validating their password.
    /// This method may also update user status to "confirmed" in the persistence store.
    /// </summary>
    /// <param name="user">The <see cref="ApplicationUser"/> to confirm.</param>
    /// <param name="password">The password used for confirmation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task VerifyUserAsync(ApplicationUser user, string password);

    /// <summary>
    /// Retrieves the OTP (One-Time Password) details associated with a user's email.
    /// </summary>
    /// <param name="email">The email address of the user.</param>
    /// <returns>
    /// A <see cref="UserOtpDetails"/> object if an OTP exists for the user; otherwise, <c>null</c>.
    /// </returns>
    Task<UserOtpDetails?> GetUserOtpDetailsByEmailAsync(string email);

    /// <summary>
    /// Retrieves all role names assigned to the specified user.
    /// </summary>
    /// <param name="user">The <see cref="ApplicationUser"/> whose roles are being retrieved.</param>
    /// <returns>
    /// A collection of role names (<see cref="string"/>) associated with the user.
    /// </returns>
    Task<string> GetUserRoleAsync(ApplicationUser user);

    /// <summary>
    /// Retrieves all permissions assigned to the specified user.
    /// </summary>
    /// <param name="user">The <see cref="ApplicationUser"/> whose permissions are being retrieved.</param>
    /// <returns>
    /// A collection of permission names (<see cref="string"/>) associated with the user.
    /// </returns>
    Task<IEnumerable<string>> GetUserPermissionsAsync(ApplicationUser user);

    /// <summary>
    /// Checks whether the provided password is correct for the given user.
    /// </summary>
    /// <param name="user">The <see cref="ApplicationUser"/> to check the password for.</param>
    /// <param name="password">The password to validate.</param>
    /// <returns>
    /// <c>true</c> if the password is correct; otherwise, <c>false</c>.
    /// </returns>
    Task<bool> CheckUserPasswordAsync(ApplicationUser user, string password);

    /// <summary>
    /// Retrieves consolidated authentication information for a user by their unique identifier.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>
    /// A <see cref="UserAuthInfo"/> object containing role and permissions if the user exists; otherwise, <c>null</c>.
    /// </returns>
    Task<UserAuthInfo?> GetUserAuthInfoAsync(Guid userId);

    #endregion
}
