using KOD.Domain.Audit;
using KOD.Domain.News;
using KOD.Domain.Sessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KOD.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps news, audit entries and sessions.
/// </summary>
internal sealed class MiscConfiguration :
    IEntityTypeConfiguration<NewsArticle>,
    IEntityTypeConfiguration<AuditEntry>,
    IEntityTypeConfiguration<UserSession>
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
    }

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

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("user_sessions");
        builder.Property(static s => s.Id).ValueGeneratedNever();
        builder.Property(static s => s.IpAddress).HasMaxLength(64);
        builder.Property(static s => s.UserAgent).HasMaxLength(512);
        builder.HasIndex(static s => s.UserId);
        builder.HasIndex(static s => s.ExpiresAt);
    }
}
