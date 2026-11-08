using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ODC.Web.Shared.Domain.Audit;

namespace ODC.Web.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the audit trail.
/// </summary>
internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_entries");
        builder.Property(static e => e.Action).HasMaxLength(64);
        builder.Property(static e => e.UserName).HasMaxLength(256).HasColumnType("citext");
        builder.Property(static e => e.EntityType).HasMaxLength(64);
        builder.Property(static e => e.EntityId).HasMaxLength(64);
        builder.Property(static e => e.Details).HasMaxLength(2000).HasColumnType("citext");
        builder.Property(static e => e.IpAddress).HasMaxLength(64);
        builder.HasIndex(static e => e.OccurredAt);
        builder.HasIndex(static e => e.Action);
        builder.HasIndex(static e => e.UserId);
    }
}
