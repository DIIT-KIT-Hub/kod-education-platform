using KOD.Domain.Entities.Auth;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KOD.Infrastructure.Implementations.Persistence.Configurations;

/// <summary>
/// Configures the <see cref="RefreshToken"/> entity properties, relationships, and database constraints.
/// </summary>
internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    #region Public methods

    /// <summary>
    /// Configures the <see cref="RefreshToken"/> entity.
    /// Sets property lengths, relationships with <see cref="ApplicationUser"/>, 
    /// and unique index constraints.
    /// </summary>
    /// <param name="builder">The builder used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.Property(u => u.Token)
            .HasMaxLength(256);

        builder.HasOne(rt => rt.User)
               .WithOne()
               .HasForeignKey<RefreshToken>(rt => rt.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(rt => rt.UserId)
               .IsUnique();
    }

    #endregion
}
