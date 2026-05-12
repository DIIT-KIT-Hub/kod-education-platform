using KOD.Application.DTOs.Auth;
using KOD.Domain.Entities.Users;

namespace KOD.Application.Mappings;

/// <summary>
/// Provides mapping methods for converting <see cref="UserAuthInfo"/> entities to authentication-related DTOs.
/// </summary>
public static class UserAuthInfoMappings
{
    /// <summary>
    /// Converts a <see cref="UserAuthInfo"/> entity to a <see cref="UserAuthInfoDto"/>.
    /// </summary>
    /// <param name="userAuthInfo">The user authentication information entity to convert.</param>
    /// <returns>A <see cref="UserAuthInfoDto"/> containing the user's identifier, role, and permissions.</returns>
    public static UserAuthInfoDto ToDto(this UserAuthInfo userAuthInfo)
       => new UserAuthInfoDto(userAuthInfo.Id, userAuthInfo.Role!, userAuthInfo.Permissions!);
}
