using KOD.Domain.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KOD.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="OrganizationUnit"/> and <see cref="UnitType"/>.
/// </summary>
internal sealed class OrganizationUnitConfiguration : IEntityTypeConfiguration<OrganizationUnit>, IEntityTypeConfiguration<UnitType>
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

        builder.HasOne(static u => u.Type)
            .WithMany()
            .HasForeignKey(static u => u.TypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UnitType> builder)
    {
        builder.ToTable("unit_types");
        builder.Property(static t => t.Code).HasMaxLength(UnitType.CodeMaxLength).HasColumnType("citext");
        builder.Property(static t => t.Name).HasMaxLength(UnitType.NameMaxLength).HasColumnType("citext");
        builder.Property(static t => t.Description).HasMaxLength(UnitType.DescriptionMaxLength);
        builder.Property(static t => t.Icon).HasMaxLength(UnitType.IconMaxLength);
        builder.HasIndex(static t => t.Code).IsUnique();
        builder.HasIndex(static t => t.Name).IsUnique();
    }
}
