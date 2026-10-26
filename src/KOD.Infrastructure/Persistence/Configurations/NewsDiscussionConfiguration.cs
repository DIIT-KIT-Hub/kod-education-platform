using KOD.Domain.Media;
using KOD.Domain.News;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KOD.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the comments, reactions and views of news articles, and the uploaded images.
/// </summary>
internal sealed class NewsDiscussionConfiguration :
    IEntityTypeConfiguration<NewsComment>,
    IEntityTypeConfiguration<NewsReaction>,
    IEntityTypeConfiguration<NewsView>,
    IEntityTypeConfiguration<MediaFile>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<NewsComment> builder)
    {
        builder.ToTable("news_comments");
        builder.Property(static c => c.Body).HasMaxLength(NewsComment.BodyMaxLength);
        builder.HasIndex(static c => new { c.ArticleId, c.ParentId, c.CreatedAt });

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

        builder.HasOne<Domain.Users.User>()
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

        builder.HasOne<Domain.Users.User>()
            .WithMany()
            .HasForeignKey(static v => v.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

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
