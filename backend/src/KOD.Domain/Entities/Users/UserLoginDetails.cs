namespace KOD.Domain.Entities.Users;

public class UserLoginDetails
{
    public Guid Id { get; set; }
    public string? PasswordHash { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? Role { get; set; }
    public IEnumerable<string>? Permissions { get; set; }
}
