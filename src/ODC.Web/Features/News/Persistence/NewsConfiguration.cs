using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ODC.Web.Features.News.Domain;
using ODC.Web.Shared.Domain.Users;

namespace ODC.Web.Features.News.Persistence;

/// <summary>
/// Maps news articles with their comments, reactions and views.
/// </summary>
internal sealed class NewsConfiguration :
    IEntityTypeConfiguration<NewsArticle>,
    IEntityTypeConfiguration<NewsComment>,
    IEntityTypeConfiguration<NewsReaction>,
    IEntityTypeConfiguration<NewsView>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<NewsArticle> builder)
    {
        builder.ToTable("news_articles");
        builder.Property(static a => a.Slug).HasMaxLength(NewsArticle.SlugMaxLength);
        builder.Property(static a => a.Title).HasMaxLength(NewsArticle.TitleMaxLength).HasColumnType("citext");
        builder.Property(static a => a.Summary).HasMaxLength(NewsArticle.SummaryMaxLength).HasColumnType("citext");
        builder.Property(static a => a.Body).HasMaxLength(NewsArticle.BodyMaxLength);
        builder.Property(static a => a.CoverImageUrl).HasMaxLength(256);
        builder.Ignore(static a => a.IsPublished);
        builder.HasIndex(static a => a.Slug).IsUnique();
        builder.HasIndex(static a => new { a.IsPinned, a.PublishedAt });

        builder.HasOne(static a => a.Author)
            .WithMany()
            .HasForeignKey(static a => a.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(static a => a.Organization)
            .WithMany()
            .HasForeignKey(static a => a.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<NewsComment> builder)
    {
        builder.ToTable("news_comments");
        builder.Property(static c => c.Body).HasMaxLength(NewsComment.BodyMaxLength);
        builder.HasIndex(static c => new { c.ArticleId, c.ParentId, c.CreatedAt });
        builder.HasIndex(static c => new { c.ArticleId, c.RootId });

        builder.HasOne<NewsArticle>()
            .WithMany()
            .HasForeignKey(static c => c.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(static c => c.Author)
            .WithMany()
            .HasForeignKey(static c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<NewsComment>()
            .WithMany()
            .HasForeignKey(static c => c.ParentId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<NewsReaction> builder)
    {
        builder.ToTable("news_reactions");

        // One reaction per reader and target: the article itself (no comment) or one of its comments. Two partial indexes
        // instead of NULLS NOT DISTINCT, which needs PostgreSQL 15.
        builder.HasIndex(static r => new { r.ArticleId, r.UserId })
            .IsUnique()
            .HasFilter("comment_id IS NULL")
            .HasDatabaseName("ix_news_reactions_article_reader");
        builder.HasIndex(static r => new { r.CommentId, r.UserId })
            .IsUnique()
            .HasFilter("comment_id IS NOT NULL")
            .HasDatabaseName("ix_news_reactions_comment_reader");
        builder.HasIndex(static r => r.ArticleId);

        builder.HasOne<NewsArticle>()
            .WithMany()
            .HasForeignKey(static r => r.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<NewsComment>()
            .WithMany()
            .HasForeignKey(static r => r.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Member>()
            .WithMany()
            .HasForeignKey(static r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<NewsView> builder)
    {
        builder.ToTable("news_views");
        builder.HasKey(static v => new { v.ArticleId, v.UserId });

        builder.HasOne<NewsArticle>()
            .WithMany()
            .HasForeignKey(static v => v.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Member>()
            .WithMany()
            .HasForeignKey(static v => v.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
