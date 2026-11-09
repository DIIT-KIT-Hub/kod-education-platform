using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ODC.Web.Shared.Domain.Access;

namespace ODC.Web.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the role catalog of the master database: <c>roles</c> and their permissions (<c>role_claims</c>).
/// </summary>
internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>, IEntityTypeConfiguration<RolePermission>
{
    /// <summary>
    /// Maximum length of a permission type and code.
    /// </summary>
    public const int ClaimMaxLength = 64;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(static r => r.Id);
        builder.Property(static r => r.Code).HasMaxLength(Role.CodeMaxLength);
        builder.Property(static r => r.Name).HasMaxLength(Role.NameMaxLength).HasColumnType("citext").IsRequired();
        builder.Property(static r => r.NormalizedName).HasMaxLength(Role.NameMaxLength).IsRequired();
        builder.Property(static r => r.ConcurrencyStamp).IsConcurrencyToken();
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
        builder.HasKey(static p => p.Id);
        builder.Property(static p => p.ClaimType).HasMaxLength(ClaimMaxLength).IsRequired();
        builder.Property(static p => p.ClaimValue).HasMaxLength(ClaimMaxLength).IsRequired();
        builder.HasIndex(static p => new { p.RoleId, p.ClaimType, p.ClaimValue }).IsUnique();
    }
}
