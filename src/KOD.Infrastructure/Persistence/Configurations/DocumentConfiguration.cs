using KOD.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KOD.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the document aggregate.
/// </summary>
internal sealed class DocumentConfiguration :
    IEntityTypeConfiguration<Document>,
    IEntityTypeConfiguration<DocumentSigner>,
    IEntityTypeConfiguration<DocumentSignature>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");
        builder.Property(static d => d.RegistrationNumber).HasMaxLength(32);
        builder.Property(static d => d.Title).HasMaxLength(Document.TitleMaxLength).HasColumnType("citext");
        builder.Property(static d => d.Description).HasMaxLength(Document.DescriptionMaxLength);

        // PostgreSQL "xmin" system column as the optimistic concurrency token.
        builder.Property<uint>("Version").IsRowVersion();
        builder.Ignore(static d => d.FirstSignature);
        builder.Ignore(static d => d.LatestSignature);

        ConfigureFile(builder.ComplexProperty(static d => d.File));

        builder.HasIndex(static d => d.RegistrationNumber).IsUnique();
        builder.HasIndex(static d => d.AuthorId);
        builder.HasIndex(static d => d.OrganizationUnitId);
        builder.HasIndex(static d => new { d.Status, d.CreatedAt });

        builder.HasOne(static d => d.Author)
            .WithMany()
            .HasForeignKey(static d => d.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(static d => d.OrganizationUnit)
            .WithMany()
            .HasForeignKey(static d => d.OrganizationUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(static d => d.Signers)
            .WithOne()
            .HasForeignKey(static s => s.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(static d => d.Signatures)
            .WithOne()
            .HasForeignKey(static s => s.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DocumentSigner> builder)
    {
        builder.ToTable("document_signers");
        builder.Property(static s => s.Comment).HasMaxLength(DocumentSigner.CommentMaxLength);
        builder.HasIndex(static s => new { s.DocumentId, s.Order }).IsUnique();
        builder.HasIndex(static s => new { s.UserId, s.Status });

        builder.HasOne(static s => s.User)
            .WithMany()
            .HasForeignKey(static s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DocumentSignature> builder)
    {
        builder.ToTable("document_signatures");
        builder.Property(static s => s.Level).HasMaxLength(32);
        builder.HasIndex(static s => s.DocumentId);
        builder.HasIndex(static s => s.UserId);

        ConfigureFile(builder.ComplexProperty(static s => s.File));
        builder.ComplexProperty(static s => s.Certificate, static c =>
        {
            c.Property(static x => x.SubjectFullName).HasColumnName("cert_subject").HasMaxLength(256);
            c.Property(static x => x.SubjectOrganization).HasColumnName("cert_organization").HasMaxLength(256);
            c.Property(static x => x.SubjectTaxId).HasColumnName("cert_tax_id").HasMaxLength(32);
            c.Property(static x => x.IssuerName).HasColumnName("cert_issuer").HasMaxLength(256);
            c.Property(static x => x.SerialNumber).HasColumnName("cert_serial").HasMaxLength(128);
            c.Property(static x => x.ValidFrom).HasColumnName("cert_valid_from");
            c.Property(static x => x.ValidTo).HasColumnName("cert_valid_to");
        });

        builder.HasOne(static s => s.User)
            .WithMany()
            .HasForeignKey(static s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    /// <summary>
    /// Maps the columns of a <see cref="StoredFile"/> complex property.
    /// </summary>
    /// <param name="file">The complex property builder.</param>
    private static void ConfigureFile(ComplexPropertyBuilder<StoredFile> file)
    {
        file.Property(static f => f.FileName).HasColumnName("file_name").HasMaxLength(StoredFile.FileNameMaxLength);
        file.Property(static f => f.ContentType).HasColumnName("file_content_type").HasMaxLength(128);
        file.Property(static f => f.Size).HasColumnName("file_size");
        file.Property(static f => f.Sha256).HasColumnName("file_sha256").HasMaxLength(64).IsFixedLength();
        file.Property(static f => f.StorageKey).HasColumnName("file_storage_key").HasMaxLength(512);
        file.Ignore(static f => f.IsPdf);
    }
}
