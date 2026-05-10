using System.Globalization;

using KOD.Application.Abstractions.Persitence.Transactions;
using KOD.Domain.Entities.Identity;
using KOD.Domain.Entities.Users;
using KOD.Domain.Repositories;

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

    /// <summary>
    /// The ASP.NET Core Identity role manager for <see cref="IdentityRole{TKey}"/>.
    /// </summary>
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityRepository"/> class with the specified transaction manager and user manager.
    /// </summary>
    /// <param name="transactionManager">The transaction manager.</param>
    /// <param name="userManager">The user manager for handling user operations.</param>
    /// <param name="roleManager">The role manager for handling role operations.</param>
    public IdentityRepository(ITransactionManager transactionManager, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager)
    {
        _transactionManager = transactionManager;
        _userManager = userManager;
        _roleManager = roleManager;
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

        var role = await GetUserRoleAsync(user);

        var permissions = await GetUserPermissionsAsync(user);

        return new UserLoginDetails()
        {
            Id = user.Id,
            PasswordHash = user.PasswordHash,
            EmailConfirmed = user.EmailConfirmed,
            Role = role,
            Permissions = permissions
        };
    }

    /// <inheritdoc />
    public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
        => await _userManager.FindByEmailAsync(email);

    /// <inheritdoc />
    public async Task<string> GetUserRoleAsync(ApplicationUser user)
        => (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? string.Empty;

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetUserPermissionsAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var permissions = new List<string>();

        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);

            if (role is null)
            {
                continue;
            }

            var roleClaims = await _roleManager.GetClaimsAsync(role);

            permissions.AddRange(
                roleClaims
                    .Where(c => c.Type == "permission")
                    .Select(c => c.Value)
            );
        }

        var userClaims = await _userManager.GetClaimsAsync(user);

        permissions.AddRange(
            userClaims
                .Where(c => c.Type == "permission")
                .Select(c => c.Value)
        );

        return [.. permissions.Distinct()];
    }

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

    /// <inheritdoc />
    public async Task<UserAuthInfo?> GetUserAuthInfoAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user == null)
        {
            return null;
        }

        var role = await GetUserRoleAsync(user);

        var permissions = await GetUserPermissionsAsync(user);

        return new UserAuthInfo() { Id = userId, Role = role, Permissions = permissions };
    }

    #endregion
}
