using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ODC.Web.Features.Notifications.Domain;
using ODC.Web.Shared.Domain.Users;

namespace ODC.Web.Features.Notifications.Persistence;

/// <summary>
/// Maps the notifications of users.
/// </summary>
internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.Property(static n => n.Subject).HasMaxLength(Notification.SubjectMaxLength);
        builder.Property(static n => n.Preview).HasMaxLength(Notification.PreviewMaxLength);
        builder.Property(static n => n.Code).HasMaxLength(Notification.CodeMaxLength);
        builder.Property(static n => n.Link).HasMaxLength(Notification.LinkMaxLength);

        // The panel reads the newest of a user; the badge counts the unread ones; a chat finds its unread notification.
        builder.HasIndex(static n => new { n.UserId, n.CreatedAt });
        builder.HasIndex(static n => new { n.UserId, n.EntityId });
        builder.HasIndex(static n => n.UserId).HasFilter("read_at IS NULL").HasDatabaseName("ix_notifications_user_id_unread");

        builder.HasOne<Member>()
            .WithMany()
            .HasForeignKey(static n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(static n => n.Actor)
            .WithMany()
            .HasForeignKey(static n => n.ActorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
