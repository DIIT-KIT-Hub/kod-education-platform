using KOD.Domain.Constants;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KOD.Infrastructure.Implementations.Persistence.Configurations;

internal sealed class AspNetRoleClaimsConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRoleClaim<Guid>> builder)
    {
        var adminRoleId = Guid.Parse("a953e99c-2320-4616-b741-b770f2d6bd17");
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
}
