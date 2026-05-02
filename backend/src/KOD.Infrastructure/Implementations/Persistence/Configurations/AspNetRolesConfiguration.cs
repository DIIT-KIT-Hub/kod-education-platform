using System.Globalization;

using KOD.Domain.Entities.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KOD.Infrastructure.Implementations.Persistence.Configurations;

internal sealed class AspNetRolesConfiguration : IEntityTypeConfiguration<IdentityRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRole<Guid>> builder)
    {
        IdentityRole<Guid>[] roles = [
            new IdentityRole<Guid>
            {
                Id = Guid.Parse("a953e99c-2320-4616-b741-b770f2d6bd17"),
                Name = Roles.Admin,
                NormalizedName = Roles.Admin.ToUpper(CultureInfo.InvariantCulture),
                ConcurrencyStamp = "4c9c0dc2-19c5-4da3-8fed-b4bf14f153e6"
            },
            new IdentityRole<Guid>
            {
                Id = Guid.Parse("9597b2ae-8059-4b51-beff-c0b295e8a5fa"),
                Name = Roles.User,
                NormalizedName = Roles.User.ToUpper(CultureInfo.InvariantCulture),
                ConcurrencyStamp = "bcb0c814-244c-4f23-ae5c-481fc035b615"
            }
        ];

        builder.HasData(roles);
    }
}
