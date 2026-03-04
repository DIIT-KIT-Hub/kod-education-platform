using KOD.Application.Abstractions.Persitence.Repositories.Identity;
using KOD.Application.Abstractions.Persitence.Transactions;
using KOD.Application.DTOs.Otp;
using KOD.Application.Result;
using KOD.Domain.Entities.Identity;
using KOD.Domain.Entities.Users;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace KOD.Infrastructure.Implementations.Persistence.Repositories.Identity;

/// <summary>
/// Implements <see cref="IIdentityRepository"/> for managing user identity data in the database.
/// </summary>
internal sealed class IdentityRepository : IIdentityRepository
{
    #region Private fields

    /// <summary>
    /// The transaction manager used to handle database transactions.
    /// </summary>
    private readonly ITransactionManager _transactionManager;

    /// <summary>
    /// The ASP.NET Core Identity user manager for <see cref="ApplicationUser"/>.
    /// </summary>
    private readonly UserManager<ApplicationUser> _userManager;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityRepository"/> class with the specified transaction manager and user manager.
    /// </summary>
    /// <param name="transactionManager">The transaction manager.</param>
    /// <param name="userManager">The user manager for handling user operations.</param>
    public IdentityRepository(ITransactionManager transactionManager, UserManager<ApplicationUser> userManager)
    {
        _transactionManager = transactionManager;
        _userManager = userManager;
    }

    #endregion

    #region Public methods

    /// <inheritdoc />
    public async Task<ApiResult<ApplicationUser>> GetUserByUsernameAsync(string username)
    {
        var user = await _userManager.FindByNameAsync(username);

        if (user is null)
        {
            return ApiResult<ApplicationUser>.Failure(StatusCodes.Status404NotFound, "User not found.");
        }

        return ApiResult<ApplicationUser>.Success(user);
    }

    /// <inheritdoc />
    public async Task<ApiResult<IEnumerable<string>>> GetUserRolesAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Count == 0)
        {
            return ApiResult<IEnumerable<string>>.Failure(StatusCodes.Status404NotFound, "User roles not found.");
        }

        return ApiResult<IEnumerable<string>>.Success(roles);
    }

    /// <inheritdoc />
    public async Task<ApiResult<bool>> ConfirmUserAsync(ApplicationUser user, string password)
    {
        await using var transaction = await _transactionManager.BeginTransactionAsync();

        if (user.IsVerified)
        {
            return ApiResult<bool>.Failure(StatusCodes.Status400BadRequest, "User already verified.");
        }

        user.IsVerified = true;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return ApiResult<bool>.Failure(StatusCodes.Status500InternalServerError, "Error occurred during user verification updating.");
        }

        var passwordResult = await _userManager.AddPasswordAsync(user, password);

        if (!passwordResult.Succeeded)
        {
            await transaction.RollbackAsync();
            return ApiResult<bool>.Failure(StatusCodes.Status500InternalServerError, "Error occured during user password updating.");
        }

        if (!await _userManager.IsInRoleAsync(user, Roles.Admin))
        {
            var roleResult = await _userManager.AddToRoleAsync(user, Roles.Admin);
            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return ApiResult<bool>.Failure(StatusCodes.Status500InternalServerError, "Error occured during user role updating.");
            }
        }

        await transaction.CommitAsync();
        return ApiResult<bool>.Success(true);
    }

    /// <inheritdoc />
    public async Task<ApiResult<UserOtpDto>> GetUserForOtpAsync(string username)
    {
        var user = await _userManager.FindByNameAsync(username);

        if (user == null)
        {
            return ApiResult<UserOtpDto>.Failure(StatusCodes.Status404NotFound, "User not found.");
        }

        if (user.IsVerified)
        {
            return ApiResult<UserOtpDto>.Failure(StatusCodes.Status409Conflict, "User already verified.");
        }

        return ApiResult<UserOtpDto>.Success(new UserOtpDto(user.Id, user.Email!));
    }

    /// <inheritdoc />
    public async Task<ApiResult<bool>> CheckUserPasswordAsync(ApplicationUser user, string password)
    {
        var passwordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
        {
            return ApiResult<bool>.Failure(StatusCodes.Status400BadRequest, "Login or password is incorrect.");
        }

        return ApiResult<bool>.Success(true);
    }

    #endregion
}
