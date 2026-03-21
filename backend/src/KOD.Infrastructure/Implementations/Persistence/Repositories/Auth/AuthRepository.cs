using KOD.Domain.Entities.Auth;
using KOD.Domain.Repositories;
using KOD.Domain.ValueObjects.Auth;
using KOD.Domain.ValueObjects.Users;
using KOD.Infrastructure.Implementations.Persistence.Database;

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
    public async Task<RefreshTokenDetails?> GetRefreshTokenDetailsByValueAsync(string token)
        => await _dbContext.RefreshTokens
        .Where(rt => rt.Token == token)
        .Select(rt => new RefreshTokenDetails(
            rt.Token,
            rt.ExpiresAt,
            new UserIdentity(rt.User!.Id, rt.User.Email!)
        ))
        .FirstOrDefaultAsync();

    /// <inheritdoc />
    public async Task UpdateRefreshTokenAsync(RefreshToken refreshToken)
    {
        var existingToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.UserId == refreshToken.UserId);

        if (existingToken != null)
        {
            existingToken.Token = refreshToken.Token;
            existingToken.ExpiresAt = refreshToken.ExpiresAt;
            _dbContext.RefreshTokens.Update(existingToken);
        }
        else
        {
            await _dbContext.RefreshTokens.AddAsync(refreshToken);
        }

        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteRefreshTokenByValueAsync(string token)
    {
        var refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);

        if (refreshToken != null)
        {
            _dbContext.RefreshTokens.Remove(refreshToken);

            await _dbContext.SaveChangesAsync();
        }
    }

    #endregion
}
