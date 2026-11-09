using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Users;

namespace ODC.Web.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the people of an organization's database: <c>members</c> (copies of the accounts' profiles), their positions
/// (<c>staff_positions</c>) and individual permission adjustments (<c>permission_overrides</c>). Roles are referred to by
/// identifier only: they live in the master database.
/// </summary>
internal sealed class MemberConfiguration :
    IEntityTypeConfiguration<Member>,
    IEntityTypeConfiguration<StaffPosition>,
    IEntityTypeConfiguration<UserPermissionOverride>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("members");
        builder.Property(static m => m.Email).HasMaxLength(User.EmailMaxLength).HasColumnType("citext");
        builder.Property(static m => m.LastName).HasMaxLength(User.NamePartMaxLength).HasColumnType("citext");
        builder.Property(static m => m.FirstName).HasMaxLength(User.NamePartMaxLength).HasColumnType("citext");
        builder.Property(static m => m.MiddleName).HasMaxLength(User.NamePartMaxLength).HasColumnType("citext");
        builder.Property(static m => m.PhoneNumber).HasMaxLength(32);
        builder.Ignore(static m => m.FullName);
        builder.Ignore(static m => m.ShortName);
        builder.HasIndex(static m => new { m.LastName, m.FirstName });
        builder.HasIndex(static m => m.Email);
        builder.HasMany(static m => m.Positions)
            .WithOne(static p => p.Member)
            .HasForeignKey(static p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(static m => m.PermissionOverrides)
            .WithOne()
            .HasForeignKey(static o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StaffPosition> builder)
    {
        builder.ToTable("staff_positions");
        builder.Property(static p => p.Title).HasMaxLength(StaffPosition.TitleMaxLength);
        builder.HasIndex(static p => new { p.UserId, p.OrganizationUnitId, p.RoleId }).IsUnique();
        builder.HasIndex(static p => p.OrganizationUnitId);
        builder.HasIndex(static p => p.RoleId);
        builder.HasOne(static p => p.OrganizationUnit)
            .WithMany()
            .HasForeignKey(static p => p.OrganizationUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserPermissionOverride> builder)
    {
        builder.ToTable("permission_overrides");
        builder.Property(static p => p.ClaimType).HasMaxLength(RoleConfiguration.ClaimMaxLength);
        builder.Property(static p => p.ClaimValue).HasMaxLength(RoleConfiguration.ClaimMaxLength);
        builder.HasIndex(static p => new { p.UserId, p.ClaimType, p.ClaimValue }).IsUnique();
    }
}
