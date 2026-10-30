using KOD.Domain.Access;
using KOD.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KOD.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps roles, permissions and positions onto the Identity tables <c>roles</c>, <c>role_claims</c>, <c>user_claims</c>
/// and <c>user_roles</c>.
/// </summary>
internal sealed class AccessConfiguration :
    IEntityTypeConfiguration<Role>,
    IEntityTypeConfiguration<RolePermission>,
    IEntityTypeConfiguration<UserPermissionOverride>,
    IEntityTypeConfiguration<StaffPosition>
{
    /// <summary>
    /// Maximum length of a claim type and of a permission code.
    /// </summary>
    private const int ClaimMaxLength = 64;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.Property(static r => r.Code).HasMaxLength(Role.CodeMaxLength);
        builder.Property(static r => r.Name).HasMaxLength(Role.NameMaxLength).HasColumnType("citext").IsRequired();
        builder.Property(static r => r.NormalizedName).HasMaxLength(Role.NameMaxLength).IsRequired();
        builder.Property(static r => r.Description).HasMaxLength(Role.DescriptionMaxLength);
        builder.Ignore(static r => r.IsAdministrator);
        builder.HasIndex(static r => r.Code).IsUnique();
        builder.HasIndex(static r => r.NormalizedName).HasDatabaseName("ix_roles_normalized_name");

        builder.HasMany(static r => r.Permissions)
            .WithOne()
            .HasForeignKey(static p => p.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_claims");
        builder.Property(static p => p.ClaimType).HasMaxLength(ClaimMaxLength).IsRequired();
        builder.Property(static p => p.ClaimValue).HasMaxLength(ClaimMaxLength).IsRequired();
        builder.HasIndex(static p => new { p.RoleId, p.ClaimType, p.ClaimValue }).IsUnique();
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserPermissionOverride> builder)
    {
        builder.ToTable("user_claims");
        builder.Property(static p => p.ClaimType).HasMaxLength(ClaimMaxLength).IsRequired();
        builder.Property(static p => p.ClaimValue).HasMaxLength(ClaimMaxLength).IsRequired();
        builder.HasIndex(static p => new { p.UserId, p.ClaimType, p.ClaimValue }).IsUnique();
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StaffPosition> builder)
    {
        builder.ToTable("user_roles");

        // Identity keys user roles by (user, role); a position adds the unit, so the same role may be held in several units.
        builder.HasKey(static p => p.Id);
        builder.Property(static p => p.Title).HasMaxLength(StaffPosition.TitleMaxLength);
        builder.HasIndex(static p => new { p.UserId, p.OrganizationUnitId, p.RoleId }).IsUnique();
        builder.HasIndex(static p => p.OrganizationUnitId);
        builder.HasIndex(static p => p.RoleId);

        builder.HasOne(static p => p.OrganizationUnit)
            .WithMany()
            .HasForeignKey(static p => p.OrganizationUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(static p => p.Role)
            .WithMany()
            .HasForeignKey(static p => p.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
