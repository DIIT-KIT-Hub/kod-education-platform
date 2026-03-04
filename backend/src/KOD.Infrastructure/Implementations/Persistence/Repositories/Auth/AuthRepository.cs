using KOD.Application.Abstractions.Persitence.Repositories.Auth;
using KOD.Application.Result;
using KOD.Domain.Entities.Auth;
using KOD.Infrastructure.Implementations.Persistence.Database;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace KOD.Infrastructure.Implementations.Persistence.Repositories.Auth;

/// <summary>
/// Implements <see cref="IAuthRepository"/> for managing authentication-related data, such as refresh tokens, in the database.
/// </summary>
internal sealed class AuthRepository : IAuthRepository
{
    #region Private fields

    /// <summary>
    /// The database context used to access authentication-related entities.
    /// </summary>
    private readonly ApplicationDbContext _dbContext;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthRepository"/> class with the specified database context.
    /// </summary>
    /// <param name="dbContext">The application's database context.</param>
    public AuthRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

    #endregion

    #region Public methods

    /// <inheritdoc />
    public async Task<ApiResult<RefreshToken>> GetRefreshTokenByValueAsync(string token)
    {
        var refreshToken = await _dbContext.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(rt => rt.Token == token);

        if (refreshToken == null || refreshToken.ExpiresAt <= DateTime.UtcNow)
        {
            return ApiResult<RefreshToken>.Failure(StatusCodes.Status400BadRequest, "Invalid or expired refresh token.");
        }

        return ApiResult<RefreshToken>.Success(refreshToken);
    }

    /// <inheritdoc />
    public async Task<ApiResult<bool>> UpdateUserRefreshTokenAsync(RefreshToken refreshToken)
    {
        var existingToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.UserId == refreshToken.UserId);

        if (existingToken != null)
        {
            existingToken.Token = refreshToken.Token;
            existingToken.ExpiresAt = refreshToken.ExpiresAt;
            _dbContext.RefreshTokens.Update(existingToken);
        }
        else
        {
            refreshToken.UserId = refreshToken.UserId;
            await _dbContext.RefreshTokens.AddAsync(refreshToken);
        }

        var result = await _dbContext.SaveChangesAsync();

        if (result != 1)
        {
            return ApiResult<bool>.Failure(StatusCodes.Status500InternalServerError, "Error occured during refresh token updating.");
        }

        return ApiResult<bool>.Success(true);
    }

    /// <inheritdoc />
    public async Task<ApiResult<bool>> DeleteRefreshTokenAsync(string token)
    {
        var refreshToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);

        if (refreshToken == null)
        {
            return ApiResult<bool>.Failure(StatusCodes.Status404NotFound, "Refresh token was not found.");
        }

        _dbContext.RefreshTokens.Remove(refreshToken);

        var result = await _dbContext.SaveChangesAsync();

        if (result != 1)
        {
            return ApiResult<bool>.Failure(StatusCodes.Status500InternalServerError, "Error occured during refresh token deleting.");
        }

        return ApiResult<bool>.Success(true);
    }

    #endregion
}
