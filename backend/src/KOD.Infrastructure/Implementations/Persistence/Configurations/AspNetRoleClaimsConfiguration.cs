using KOD.Domain.Constants;
using KOD.Infrastructure.Implementations.Persistence.Constants;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KOD.Infrastructure.Implementations.Persistence.Configurations;

/// <summary>
/// Seeds initial role claims (permissions) for ASP.NET Identity roles.
/// </summary>
internal sealed class AspNetRoleClaimsConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<Guid>>
{
    #region Public methods

    /// <summary>
    /// Configures the entity type and seeds default role-based permissions.
    /// </summary>
    /// <param name="builder">The builder used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<IdentityRoleClaim<Guid>> builder)
    {
        var adminRoleId = ConfigurationConstants.AdminRoleId;
        const string type = "permission";

        builder.HasData([
            new IdentityRoleClaim<Guid> { Id = 1, RoleId = adminRoleId, ClaimType = type, ClaimValue = HomeModulePermissions.Create },
            new IdentityRoleClaim<Guid> { Id = 2, RoleId = adminRoleId, ClaimType = type, ClaimValue = HomeModulePermissions.Read },
            new IdentityRoleClaim<Guid> { Id = 3, RoleId = adminRoleId, ClaimType = type, ClaimValue = HomeModulePermissions.Update },
            new IdentityRoleClaim<Guid> { Id = 4, RoleId = adminRoleId, ClaimType = type, ClaimValue = HomeModulePermissions.Delete },

            new IdentityRoleClaim<Guid> { Id = 5, RoleId = adminRoleId, ClaimType = type, ClaimValue = UserModulePermissions.Create },
            new IdentityRoleClaim<Guid> { Id = 6, RoleId = adminRoleId, ClaimType = type, ClaimValue = UserModulePermissions.Read },
            new IdentityRoleClaim<Guid> { Id = 7, RoleId = adminRoleId, ClaimType = type, ClaimValue = UserModulePermissions.Update },
            new IdentityRoleClaim<Guid> { Id = 8, RoleId = adminRoleId, ClaimType = type, ClaimValue = UserModulePermissions.Delete },
        ]);
    }

    #endregion
}
