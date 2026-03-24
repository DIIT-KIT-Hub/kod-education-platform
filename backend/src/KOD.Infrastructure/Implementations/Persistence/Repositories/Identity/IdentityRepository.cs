using System.Globalization;

using KOD.Application.Abstractions.Persitence.Transactions;
using KOD.Domain.Entities.Identity;
using KOD.Domain.Entities.Users;
using KOD.Domain.Repositories;
using KOD.Domain.ValueObjects.Users;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
    public async Task<UserLoginDetails?> GetUserLoginDetailsByEmailAsync(string email)
    {
        var user = await GetUserByEmailAsync(email);

        if (user is null)
        {
            return null;
        }

        var userRoles = await GetUserRolesAsync(user);

        return new UserLoginDetails(user.Id, user.PasswordHash!, user.EmailConfirmed, userRoles);
    }

    /// <inheritdoc />
    public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
        => await _userManager.FindByEmailAsync(email);

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetUserRolesAsync(ApplicationUser user)
        => await _userManager.GetRolesAsync(user);

    /// <inheritdoc />
    public async Task VerifyUserAsync(ApplicationUser user, string password)
    {
        await using var transaction = await _transactionManager.BeginTransactionAsync();

        user.EmailConfirmed = true;

        await _userManager.UpdateAsync(user);

        await _userManager.AddPasswordAsync(user, password);

        if (!await _userManager.IsInRoleAsync(user, Roles.User))
        {
            await _userManager.AddToRoleAsync(user, Roles.User);
        }

        await transaction.CommitAsync();
    }

    /// <inheritdoc />
    public async Task<UserOtpDetails?> GetUserOtpDetailsByEmailAsync(string email)
    {
        var normalizedEmail = email.ToUpper(CultureInfo.CurrentCulture);

        return await _userManager.Users
            .Where(x => x.NormalizedEmail == normalizedEmail)
            .Select(x => new UserOtpDetails(x.Id, x.Email!, x.EmailConfirmed))
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<bool> CheckUserPasswordAsync(ApplicationUser user, string password)
        => await _userManager.CheckPasswordAsync(user, password);


    #endregion
}
