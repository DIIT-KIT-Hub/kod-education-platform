using KOD.Application.Abstractions.Services.Auth;
using KOD.Application.DTOs.Auth;
using KOD.Application.DTOs.Tokens;
using KOD.Application.Mappings;
using KOD.Application.Results;
using KOD.Domain.Mappings;
using KOD.Domain.Repositories;
using KOD.Domain.ValueObjects.Users;

namespace KOD.Infrastructure.Implementations.Services.Auth;

/// <summary>
/// Implements <see cref="IAuthService"/> for handling authentication operations such as login, token refresh, and logout.
/// </summary>
internal sealed class AuthService : IAuthService
{
    #region Private fields

    /// <summary>
    /// The JWT service used to generate access and refresh tokens.
    /// </summary>
    private readonly IJwtService _jwtService;

    /// <summary>
    /// The repository for managing refresh tokens in the database.
    /// </summary>
    private readonly IAuthRepository _authRepository;

    /// <summary>
    /// The repository for accessing user identity data.
    /// </summary>
    private readonly IIdentityRepository _identityRepository;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthService"/> class with the specified services and repositories.
    /// </summary>
    /// <param name="jwtService">The JWT service.</param>
    /// <param name="authRepository">The authentication repository.</param>
    /// <param name="identityRepository">The identity repository.</param>
    public AuthService(IJwtService jwtService, IAuthRepository authRepository, IIdentityRepository identityRepository)
    {
        _jwtService = jwtService;
        _authRepository = authRepository;
        _identityRepository = identityRepository;
    }

    #endregion

    #region Public methods

    /// <inheritdoc />
    public async Task<Result<TokenResponseDto>> LoginAsync(LoginRequestDto request)
    {
        var userDetails = await _identityRepository.GetUserLoginDetailsByEmailAsync(request.Email);

        if (userDetails is null)
        {
            return Result<TokenResponseDto>.Failure(Errors.NotFound("User details"));
        }

        if (!userDetails.EmailConfirmed)
        {
            return Result<TokenResponseDto>.Failure(Errors.Unauthorized("User not verified"));
        }

        var user = userDetails.ToEntity();

        var checkPasswordResult = await _identityRepository.CheckUserPasswordAsync(user, request.Password);
        if (!checkPasswordResult)
        {
            return Result<TokenResponseDto>.Failure(Errors.Validation("Login or password is probably mistaken."));
        }

        return Result<TokenResponseDto>.Success(await GenerateTokens(userDetails));
    }

    /// <inheritdoc />
    public async Task<Result<TokenResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        var refreshTokenDetails = await _authRepository.GetRefreshTokenDetailsByValueAsync(request.RefreshToken);

        if (refreshTokenDetails is null)
        {
            return Result<TokenResponseDto>.Failure(Errors.NotFound("Refresh token details"));
        }

        var userDetails = await _identityRepository.GetUserLoginDetailsByEmailAsync(refreshTokenDetails.User.Email);

        if (userDetails is null)
        {
            return Result<TokenResponseDto>.Failure(Errors.NotFound("User details"));
        }

        return Result<TokenResponseDto>.Success(await GenerateTokens(userDetails));
    }

    /// <inheritdoc />
    public async Task<Result> LogoutAsync(RefreshTokenRequestDto request)
    {
        await _authRepository.DeleteRefreshTokenByValueAsync(request.RefreshToken);

        return Result.Success();
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Generates access and refresh tokens for the specified user and updates the refresh token in the database.
    /// </summary>
    /// <param name="user">The user for whom the tokens are generated.</param>
    /// <param name="roles">The roles assigned to the user.</param>
    /// <returns>An <see cref="ApiResult{T}"/> containing the generated access and refresh tokens, or a failure result if updating the refresh token fails.</returns>
    private async Task<TokenResponseDto> GenerateTokens(UserLoginDetails userLoginDetails)
    {
        var accessToken = _jwtService.GenerateAccessToken(userLoginDetails);
        var refreshToken = _jwtService.GenerateRefreshToken();

        await _authRepository.UpdateRefreshTokenAsync(refreshToken.ToEntity(userLoginDetails.Id));

        return new TokenResponseDto(accessToken.Token, accessToken.ExpiresAt, refreshToken.Token, refreshToken.ExpiresAt);
    }

    #endregion
}
