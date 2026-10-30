using KOD.Domain.Users;
using KOD.Infrastructure.Identity.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KOD.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="RefreshTokenFamily"/>.
/// </summary>
internal sealed class RefreshTokenFamilyConfiguration : IEntityTypeConfiguration<RefreshTokenFamily>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RefreshTokenFamily> builder)
    {
        builder.ToTable("refresh_token_families");
        builder.Property(static f => f.ProtectedKey).HasMaxLength(512);

        // A rotation updates the row only while it still holds the presented generation: of parallel refreshes one wins.
        builder.Property(static f => f.Generation).IsConcurrencyToken();
        builder.Property(static f => f.SecurityStamp).HasMaxLength(256);
        builder.Property(static f => f.AuthenticationMethod).HasMaxLength(32);
        builder.Property(static f => f.IpAddress).HasMaxLength(64);
        builder.Property(static f => f.UserAgent).HasMaxLength(RefreshTokenFamily.UserAgentMaxLength);
        builder.HasIndex(static f => f.UserId);
        builder.HasIndex(static f => f.ExpiresAt);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(static f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
