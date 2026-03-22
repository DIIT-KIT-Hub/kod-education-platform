using KOD.Application.Abstractions.Services.Identity;
using KOD.Application.DTOs.Users;
using KOD.Application.Mappings;
using KOD.Application.Results;
using KOD.Domain.Repositories;

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
    public async Task<Result<bool>> VerifyUserAsync(string email, string password)
    {
        var user = await _identityRepository.GetUserByEmailAsync(email);

        if (user is null)
        {
            return Result<bool>.Failure(Errors.NotFound("User"));
        }

        await _identityRepository.VerifyUserAsync(user, password);

        return Result<bool>.Success(true);
    }

    /// <inheritdoc />
    public async Task<Result<UserOtpDetailsDto>> GetUserOtpDetailsByEmailAsync(string email)
    {
        var userOtpDetails = await _identityRepository.GetUserOtpDetailsByEmailAsync(email);

        if (userOtpDetails is null)
        {
            return Result<UserOtpDetailsDto>.Failure(Errors.NotFound("User otp details"));
        }

        return Result<UserOtpDetailsDto>.Success(userOtpDetails.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<bool>> CheckUserExistenceByEmailAsync(string email)
    {
        var user = await _identityRepository.GetUserByEmailAsync(email);

        if(user is null)
        {
            return Result<bool>.Failure(Errors.NotFound("User"));
        }

        return Result<bool>.Success(true);
    }

    /// <inheritdoc />
    public async Task<Result<bool>> CheckUserVerificationByEmailAsync(string email)
    {
        var user = await _identityRepository.GetUserByEmailAsync(email);
        
        if(user is null)
        {
            return Result<bool>.Failure(Errors.NotFound("User"));
        }

        if (user.EmailConfirmed)
        {
            return Result<bool>.Failure(Errors.Conflict("User already verified."));
        }

        return Result<bool>.Success(user.EmailConfirmed);
    }

    #endregion
}
