using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ODC.Web.Infrastructure.Files;

namespace ODC.Web.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="PlatformSecret"/>.
/// </summary>
internal sealed class PlatformSecretConfiguration : IEntityTypeConfiguration<PlatformSecret>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PlatformSecret> builder)
    {
        builder.ToTable("platform_secrets");
        builder.HasKey(static s => s.Name);
        builder.Property(static s => s.Name).HasMaxLength(PlatformSecret.NameMaxLength);
        builder.Property(static s => s.Value).HasMaxLength(1024);
    }
}
