using KOD.Domain.Access;
using KOD.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KOD.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps roles, permissions and positions.
/// </summary>
internal sealed class AccessConfiguration :
    IEntityTypeConfiguration<Role>,
    IEntityTypeConfiguration<RolePermission>,
    IEntityTypeConfiguration<UserPermissionOverride>,
    IEntityTypeConfiguration<StaffPosition>
{
    /// <summary>
    /// Maximum length of a permission code.
    /// </summary>
    private const int PermissionMaxLength = 64;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.Property(static r => r.Code).HasMaxLength(Role.CodeMaxLength);
        builder.Property(static r => r.Name).HasMaxLength(Role.NameMaxLength).HasColumnType("citext");
        builder.Property(static r => r.Description).HasMaxLength(Role.DescriptionMaxLength);
        builder.Ignore(static r => r.IsAdministrator);
        builder.HasIndex(static r => r.Code).IsUnique();

        builder.HasMany(static r => r.Permissions)
            .WithOne()
            .HasForeignKey(static p => p.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");
        builder.HasKey(static p => new { p.RoleId, p.Permission });
        builder.Property(static p => p.Permission).HasMaxLength(PermissionMaxLength);
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserPermissionOverride> builder)
    {
        builder.ToTable("user_permission_overrides");
        builder.HasKey(static p => new { p.UserId, p.Permission });
        builder.Property(static p => p.Permission).HasMaxLength(PermissionMaxLength);
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

        builder.HasOne(static p => p.Role)
            .WithMany()
            .HasForeignKey(static p => p.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
