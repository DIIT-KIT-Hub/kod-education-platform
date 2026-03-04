using KOD.Application.Abstractions.Persitence.Repositories.Identity;
using KOD.Application.Abstractions.Services.Identity;
using KOD.Application.DTOs.Otp;
using KOD.Application.Result;

namespace KOD.Infrastructure.Implementations.Services.Identity;

/// <summary>
/// Implements <see cref="IIdentityService"/> for managing user identity operations such as confirmation and OTP retrieval.
/// </summary>
internal sealed class IdentityService : IIdentityService
{
    #region Private fields

    /// <summary>
    /// The repository used to access user identity data.
    /// </summary>
    private readonly IIdentityRepository _identityRepository;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityService"/> class with the specified identity repository.
    /// </summary>
    /// <param name="identityRepository">The identity repository.</param>
    public IdentityService(IIdentityRepository identityRepository) => _identityRepository = identityRepository;

    #endregion

    #region Public methods

    /// <inheritdoc />
    public async Task<ApiResult<bool>> ConfirmUserAsync(string username, string password)
    {
        var userResult = await _identityRepository.GetUserByUsernameAsync(username);

        if (!userResult.IsSuccess)
        {
            return ApiResult<bool>.Failure(userResult.StatusCode, userResult.Message!);
        }

        var registrationResult = await _identityRepository.ConfirmUserAsync(userResult.Data!, password);

        if (!registrationResult.IsSuccess)
        {
            return ApiResult<bool>.Failure(registrationResult.StatusCode, registrationResult.Message!);
        }

        return ApiResult<bool>.Success(true);
    }

    /// <inheritdoc />
    public async Task<ApiResult<UserOtpDto>> GetUserForOtpAsync(string username)
        => await _identityRepository.GetUserForOtpAsync(username);

    #endregion
}
