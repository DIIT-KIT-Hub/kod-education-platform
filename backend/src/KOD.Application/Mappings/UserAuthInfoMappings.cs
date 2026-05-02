using KOD.Application.DTOs.Auth;
using KOD.Domain.Entities.Users;

namespace KOD.Application.Mappings;

public static class UserAuthInfoMappings
{
    public static UserAuthInfoDto ToDto(this UserAuthInfo userAuthInfo)
       => new UserAuthInfoDto(userAuthInfo.Id, userAuthInfo.Role!, userAuthInfo.Permissions!);
}
