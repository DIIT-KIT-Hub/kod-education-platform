using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ODC.Web.Shared.Domain.Organization;

namespace ODC.Web.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the catalog of <see cref="UnitType"/> (master database).
/// </summary>
internal sealed class UnitTypeConfiguration : IEntityTypeConfiguration<UnitType>
{
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
