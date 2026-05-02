using KOD.Domain.Entities.Users;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KOD.Infrastructure.Implementations.Persistence.Configurations;

/// <summary>
/// Configures the <see cref="ApplicationUser"/> entity properties and constraints for the database schema.
/// </summary>
internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    #region Public methods

    /// <summary>
    /// Configures the <see cref="ApplicationUser"/> entity.
    /// Sets property lengths, default values, and other database constraints.
    /// </summary>
    /// <param name="builder">The builder used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.UserName)
            .HasMaxLength(6);

        builder.Property(u => u.NormalizedUserName)
            .HasMaxLength(6);

        builder.Property(u => u.Email)
            .HasMaxLength(32);

        builder.Property(u => u.NormalizedEmail)
            .HasMaxLength(32);

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(256);

        builder.Property(u => u.SecurityStamp)
            .HasMaxLength(256);

        builder.Property(u => u.ConcurrencyStamp)
            .HasMaxLength(256);

        builder.HasData();

        var rootUser = new ApplicationUser
        {
            Id = Guid.Parse("46b56073-6b0f-4238-ae89-a81073a4774e"),
            UserName = "000000",
            NormalizedUserName = "000000",
            Email = "root@gmail.com",
            NormalizedEmail = "root@GMAIL.COM",
            PasswordHash= "AQAAAAIAAYagAAAAEOvATbb65SPv1K+5yitp18adBYw/x9n42A/iGobWqqTs4Hb7S+8vDHHTMSj3bsGCNA==",
            EmailConfirmed = true,
            SecurityStamp = "46b56073-6b0f-4238-ae89-a81073a4774e",
            ConcurrencyStamp = "46b56073-6b0f-4238-ae89-a81073a4774e",
        };

        builder.HasData(rootUser);
    }
    #endregion
}
