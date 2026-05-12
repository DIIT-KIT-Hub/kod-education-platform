using KOD.Application.DTOs.Users;
using KOD.Domain.Entities.Users;

namespace KOD.Application.Mappings;

/// <summary>
/// Provides extension methods for mapping user-related domain value objects to application DTOs.
/// </summary>
public static class UserMappingExtensions
{
    #region Public methods

    /// <summary>
    /// Converts a <see cref="UserOtpDetails"/> domain value object into a <see cref="UserOtpDetailsDto"/>.
    /// </summary>
    /// <param name="userOtpDetails">The domain object containing user OTP information.</param>
    /// <returns>
    /// A new <see cref="UserOtpDetailsDto"/> populated with the ID, email, and verification status
    /// from the domain object.
    /// </returns>
    public static UserOtpDetailsDto ToDto(this UserOtpDetails userOtpDetails) 
        => new UserOtpDetailsDto(userOtpDetails.Id, userOtpDetails.Email, userOtpDetails.EmailConfirmed);

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
            EmailConfirmed = userLoginDetails.EmailConfirmed
        };

    #endregion
}
