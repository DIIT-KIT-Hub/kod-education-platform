using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ODC.Web.Features.News.Domain;
using ODC.Web.Shared.Domain.Tenancy;

namespace ODC.Web.Features.News.Persistence;

/// <summary>
/// Maps the index of the news of the whole cabinet (<c>general_articles</c>, master database).
/// </summary>
internal sealed class GeneralArticleConfiguration : IEntityTypeConfiguration<GeneralArticle>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GeneralArticle> builder)
    {
        builder.ToTable("general_articles");
        builder.HasKey(static a => a.ArticleId);
        builder.Property(static a => a.ArticleId).ValueGeneratedNever();
        builder.HasIndex(static a => a.OrganizationId);
        builder.HasOne<Organization>().WithMany().HasForeignKey(static a => a.OrganizationId).OnDelete(DeleteBehavior.Cascade);
    }
}
