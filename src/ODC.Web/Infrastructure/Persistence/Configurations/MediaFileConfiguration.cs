using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ODC.Web.Shared.Domain.Media;

namespace ODC.Web.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the uploaded images (master database).
/// </summary>
internal sealed class MediaFileConfiguration : IEntityTypeConfiguration<MediaFile>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MediaFile> builder)
    {
        builder.ToTable("media_files");
        builder.HasIndex(static m => m.NewsArticleId);
        builder.HasIndex(static m => new { m.Purpose, m.CreatedAt });

        // NewsArticleId refers to an article of an organization's database: no foreign key across databases. Deleting
        // an article detaches its images (NewsDesk), and MediaCleanupService removes unattached images a day later.
    }
}
