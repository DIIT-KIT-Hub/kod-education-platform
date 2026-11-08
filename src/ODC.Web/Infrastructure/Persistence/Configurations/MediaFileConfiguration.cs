using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ODC.Web.Features.News.Domain;
using ODC.Web.Shared.Domain.Media;

namespace ODC.Web.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the uploaded images.
/// </summary>
internal sealed class MediaFileConfiguration : IEntityTypeConfiguration<MediaFile>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MediaFile> builder)
    {
        builder.ToTable("media_files");
        builder.HasIndex(static m => m.NewsArticleId);
        builder.HasIndex(static m => new { m.Purpose, m.CreatedAt });

        builder.HasOne<NewsArticle>()
            .WithMany()
            .HasForeignKey(static m => m.NewsArticleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
