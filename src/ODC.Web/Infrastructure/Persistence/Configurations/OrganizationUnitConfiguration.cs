using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ODC.Web.Shared.Domain.Organization;

namespace ODC.Web.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="OrganizationUnit"/> (an organization's database). The unit type is referred to by identifier only: the
/// catalog lives in the master database.
/// </summary>
internal sealed class OrganizationUnitConfiguration : IEntityTypeConfiguration<OrganizationUnit>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OrganizationUnit> builder)
    {
        builder.ToTable("organization_units");
        builder.Property(static u => u.Code).HasMaxLength(OrganizationUnit.CodeMaxLength).HasColumnType("citext");
        builder.Property(static u => u.Name).HasMaxLength(OrganizationUnit.NameMaxLength).HasColumnType("citext");
        builder.Property(static u => u.ShortName).HasMaxLength(OrganizationUnit.ShortNameMaxLength);
        builder.HasIndex(static u => u.Code).IsUnique();
        builder.HasIndex(static u => u.ParentId);
        builder.HasIndex(static u => u.TypeId);
        builder.HasOne(static u => u.Parent)
            .WithMany(static u => u.Children)
            .HasForeignKey(static u => u.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
