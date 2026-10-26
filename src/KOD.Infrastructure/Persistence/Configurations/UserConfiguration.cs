using KOD.Domain.Media;
using KOD.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KOD.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="User"/> and the Identity user tables.
/// </summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.Property(static u => u.Email).HasMaxLength(User.EmailMaxLength).HasColumnType("citext");
        builder.Property(static u => u.UserName).HasMaxLength(User.EmailMaxLength);
        builder.Property(static u => u.LastName).HasMaxLength(User.NamePartMaxLength).HasColumnType("citext");
        builder.Property(static u => u.FirstName).HasMaxLength(User.NamePartMaxLength).HasColumnType("citext");
        builder.Property(static u => u.MiddleName).HasMaxLength(User.NamePartMaxLength).HasColumnType("citext");
        builder.Property(static u => u.PhoneNumber).HasMaxLength(32);
        builder.Property(static u => u.Language).HasMaxLength(8);

        builder.Ignore(static u => u.FullName);
        builder.Ignore(static u => u.ShortName);

        builder.HasIndex(static u => new { u.LastName, u.FirstName });

        builder.HasMany(static u => u.Positions)
            .WithOne(static p => p.User)
            .HasForeignKey(static p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(static u => u.PermissionOverrides)
            .WithOne()
            .HasForeignKey(static o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<MediaFile>()
            .WithMany()
            .HasForeignKey(static u => u.PhotoId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
