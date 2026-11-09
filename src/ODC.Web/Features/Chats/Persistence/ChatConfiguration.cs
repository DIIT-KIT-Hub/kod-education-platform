using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ODC.Web.Features.Chats.Domain;
using ODC.Web.Shared.Domain.Users;

namespace ODC.Web.Features.Chats.Persistence;

/// <summary>
/// Maps chats, their members, messages, attachments, reactions and the messages members hid for themselves.
/// </summary>
internal sealed class ChatConfiguration :
    IEntityTypeConfiguration<Chat>,
    IEntityTypeConfiguration<ChatMember>,
    IEntityTypeConfiguration<ChatMessage>,
    IEntityTypeConfiguration<ChatAttachment>,
    IEntityTypeConfiguration<ChatReaction>,
    IEntityTypeConfiguration<HiddenChatMessage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Chat> builder)
    {
        builder.ToTable("chats");
        builder.Property(static c => c.Title).HasMaxLength(Chat.TitleMaxLength);
        builder.Property(static c => c.DirectKey).HasMaxLength(65);

        // One one-to-one chat per pair of people, even when both start it at the same moment.
        builder.HasIndex(static c => c.DirectKey).IsUnique().HasFilter("direct_key IS NOT NULL");

        // PostgreSQL "xmin": two messages sent at once cannot take the same number.
        builder.Property<uint>("Version").IsRowVersion();

        builder.HasMany(static c => c.Members)
            .WithOne()
            .HasForeignKey(static m => m.ChatId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(static c => c.Members).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ChatMember> builder)
    {
        builder.ToTable("chat_members");
        builder.HasKey(static m => new { m.ChatId, m.UserId });
        builder.HasIndex(static m => m.UserId);

        builder.HasOne(static m => m.User)
            .WithMany()
            .HasForeignKey(static m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages");
        builder.Property(static m => m.Body).HasMaxLength(ChatMessage.BodyMaxLength);
        builder.Property(static m => m.Text).HasMaxLength(ChatMessage.TextMaxLength);

        // Pages of a conversation are read by number; a resent message is found by its client identifier.
        builder.HasIndex(static m => new { m.ChatId, m.Sequence }).IsUnique();
        builder.HasIndex(static m => new { m.ChatId, m.AuthorId, m.ClientId }).IsUnique().HasFilter("client_id IS NOT NULL");

        builder.HasOne<Chat>()
            .WithMany()
            .HasForeignKey(static m => m.ChatId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(static m => m.Author)
            .WithMany()
            .HasForeignKey(static m => m.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(static m => m.ForwardedFrom)
            .WithMany()
            .HasForeignKey(static m => m.ForwardedFromId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(static m => m.Attachments)
            .WithOne()
            .HasForeignKey(static a => a.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(static m => m.Attachments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ChatAttachment> builder)
    {
        builder.ToTable("chat_attachments");
        builder.Property(static a => a.FileName).HasMaxLength(ChatAttachment.FileNameMaxLength);
        builder.Property(static a => a.ContentType).HasMaxLength(200);
        builder.Property(static a => a.StorageKey).HasMaxLength(300);
        builder.Property(static a => a.ThumbnailKey).HasMaxLength(300);
        builder.HasIndex(static a => a.ChatId);
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ChatReaction> builder)
    {
        builder.ToTable("chat_reactions");
        builder.HasKey(static r => new { r.MessageId, r.UserId });

        builder.HasOne<ChatMessage>()
            .WithMany()
            .HasForeignKey(static r => r.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Member>()
            .WithMany()
            .HasForeignKey(static r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HiddenChatMessage> builder)
    {
        builder.ToTable("chat_hidden_messages");
        builder.HasKey(static h => new { h.MessageId, h.UserId });

        // The chat list looks for the newest message a member still sees.
        builder.HasIndex(static h => new { h.UserId, h.ChatId });

        builder.HasOne<ChatMessage>()
            .WithMany()
            .HasForeignKey(static h => h.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Chat>()
            .WithMany()
            .HasForeignKey(static h => h.ChatId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Member>()
            .WithMany()
            .HasForeignKey(static h => h.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
