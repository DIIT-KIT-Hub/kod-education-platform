using Microsoft.AspNetCore.Identity;

namespace KOD.Domain.Entities.Users;

/// <summary>
/// Represents an application user with extended profile information.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{

}
