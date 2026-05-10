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
            UserId = Guid.Parse("46b56073-6b0f-4238-ae89-a81073a4774e"),
            RoleId = Guid.Parse("a953e99c-2320-4616-b741-b770f2d6bd17")
        };

        builder.HasData(rootUserRelation);
    }

    #endregion
}
