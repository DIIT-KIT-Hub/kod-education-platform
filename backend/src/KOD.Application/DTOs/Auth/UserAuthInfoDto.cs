namespace KOD.Application.DTOs.Auth;

public sealed record UserAuthInfoDto(Guid Id, string Role, IEnumerable<string> Permissions);

