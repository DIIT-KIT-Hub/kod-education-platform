using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ODC.Web.Shared.Domain.Media;
using ODC.Web.Shared.Domain.Users;

namespace ODC.Web.Infrastructure.Persistence.Configurations;

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

        // The Identity indexes (EmailIndex, UserNameIndex) in the snake_case style of the database.
        builder.HasIndex(static u => u.NormalizedEmail).HasDatabaseName("ix_users_normalized_email");
        builder.HasIndex(static u => u.NormalizedUserName).HasDatabaseName("ix_users_normalized_user_name");

        builder.HasOne<MediaFile>()
            .WithMany()
            .HasForeignKey(static u => u.PhotoId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
