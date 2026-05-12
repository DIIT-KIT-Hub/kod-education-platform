using System.Globalization;

using KOD.Domain.Entities.Identity;
using KOD.Infrastructure.Implementations.Persistence.Constants;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KOD.Infrastructure.Implementations.Persistence.Configurations;

/// <summary>
/// Seeds default ASP.NET Identity roles into the database.
/// </summary>
internal sealed class AspNetRolesConfiguration : IEntityTypeConfiguration<IdentityRole<Guid>>
{
    #region Public methods

    /// <summary>
    /// Configures the IdentityRole entity and seeds predefined roles.
    /// </summary>
    /// <param name="builder">The builder used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<IdentityRole<Guid>> builder)
    {
        IdentityRole<Guid>[] roles = [
            new IdentityRole<Guid>
            {
                Id = ConfigurationConstants.AdminRoleId,
                Name = Roles.Admin,
                NormalizedName = Roles.Admin.ToUpper(CultureInfo.InvariantCulture),
                ConcurrencyStamp = "4c9c0dc2-19c5-4da3-8fed-b4bf14f153e6"
            },
            new IdentityRole<Guid>
            {
                Id = ConfigurationConstants.UserRoleId,
                Name = Roles.User,
                NormalizedName = Roles.User.ToUpper(CultureInfo.InvariantCulture),
                ConcurrencyStamp = "bcb0c814-244c-4f23-ae5c-481fc035b615"
            }
        ];

        builder.HasData(roles);
    }

    #endregion
}
