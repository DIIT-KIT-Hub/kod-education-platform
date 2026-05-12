using KOD.Infrastructure.Implementations.Persistence.Constants;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KOD.Infrastructure.Implementations.Persistence.Configurations;

/// <summary>
/// Seeds default user-role relationships for ASP.NET Identity.
/// </summary>
internal sealed class AspNetUserRolesConfiguration : IEntityTypeConfiguration<IdentityUserRole<Guid>>
{
    #region Public methods

    /// <summary>
    /// Configures the IdentityUserRole entity and seeds initial user-role mappings.
    /// </summary>
    /// <param name="builder">The builder used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<IdentityUserRole<Guid>> builder)
    {
        var rootUserRelation = new IdentityUserRole<Guid>
        {
            UserId = ConfigurationConstants.RootUserId,
            RoleId = ConfigurationConstants.AdminRoleId
        };

        builder.HasData(rootUserRelation);
    }

    #endregion
}
