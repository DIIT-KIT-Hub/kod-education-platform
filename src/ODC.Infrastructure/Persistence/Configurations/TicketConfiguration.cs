using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ODC.Domain.Tickets;

namespace ODC.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps requests, their messages and attachments.
/// </summary>
internal sealed class TicketConfiguration :
    IEntityTypeConfiguration<Ticket>,
    IEntityTypeConfiguration<TicketMessage>,
    IEntityTypeConfiguration<TicketAttachment>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("tickets");
        builder.Property(static t => t.Subject).HasMaxLength(Ticket.SubjectMaxLength).HasColumnType("citext");

        // The number people quote ("request No. 42") comes from an identity column.
        builder.Property(static t => t.Number).UseIdentityAlwaysColumn();
        builder.HasIndex(static t => t.Number).IsUnique();
        builder.HasIndex(static t => new { t.AuthorId, t.LastActivityAt });
        builder.HasIndex(static t => new { t.Status, t.LastActivityAt });

        builder.HasOne(static t => t.Author)
            .WithMany()
            .HasForeignKey(static t => t.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(static t => t.Assignee)
            .WithMany()
            .HasForeignKey(static t => t.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TicketMessage> builder)
    {
        builder.ToTable("ticket_messages");
        builder.Property(static m => m.Body).HasMaxLength(TicketMessage.BodyMaxLength);
        builder.HasIndex(static m => new { m.TicketId, m.CreatedAt });

        builder.HasOne<Ticket>()
            .WithMany()
            .HasForeignKey(static m => m.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(static m => m.Author)
            .WithMany()
            .HasForeignKey(static m => m.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(static m => m.Attachments)
            .WithOne()
            .HasForeignKey(static a => a.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(static m => m.Attachments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TicketAttachment> builder)
    {
        builder.ToTable("ticket_attachments");
        builder.Property(static a => a.FileName).HasMaxLength(TicketAttachment.FileNameMaxLength);
        builder.Property(static a => a.ContentType).HasMaxLength(200);
        builder.Property(static a => a.StorageKey).HasMaxLength(300);
        builder.Property(static a => a.ThumbnailKey).HasMaxLength(300);
        builder.HasIndex(static a => a.TicketId);
    }
}
