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
    }

    #endregion
}
