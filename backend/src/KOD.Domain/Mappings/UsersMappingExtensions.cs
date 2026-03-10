using KOD.Domain.Entities.Users;
using KOD.Domain.ValueObjects.Users;

namespace KOD.Domain.Mappings;

/// <summary>
/// Provides extension methods for mapping user-related value objects to domain entities.
/// </summary>
public static class UsersMappingExtensions
{
    #region Public methods

    /// <summary>
    /// Converts a <see cref="UserLoginDetails"/> value object into an <see cref="ApplicationUser"/> entity.
    /// </summary>
    /// <param name="userLoginDetails">The login details value object containing user authentication data.</param>
    /// <returns>
    /// An <see cref="ApplicationUser"/> entity populated with the corresponding properties
    /// from <paramref name="userLoginDetails"/>.
    /// </returns>
    public static ApplicationUser ToEntity(this UserLoginDetails userLoginDetails) 
        => new ApplicationUser
        {
            Id = userLoginDetails.Id,
            PasswordHash = userLoginDetails.PasswordHash,
            IsVerified = userLoginDetails.IsVerified
        };

    #endregion
}
