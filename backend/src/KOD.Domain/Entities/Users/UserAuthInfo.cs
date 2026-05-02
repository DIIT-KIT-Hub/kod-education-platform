namespace KOD.Domain.Entities.Users;

public sealed class UserAuthInfo
{
    public Guid Id { get; set; }
    public string? Role { get; set; }
    public IEnumerable<string>? Permissions { get; set; }
}
