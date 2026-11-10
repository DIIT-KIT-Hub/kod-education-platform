using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ODC.Web.Shared.Domain.Organization;
using ODC.Web.Shared.Domain.Tenancy;
using ODC.Web.Shared.Domain.Users;

namespace ODC.Web.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the registry of organizations (<c>organizations</c>), the memberships of accounts (<c>memberships</c>) and their
/// participation in other organizations (<c>participations</c>) in the master database.
/// </summary>
internal sealed class TenancyConfiguration : IEntityTypeConfiguration<Organization>, IEntityTypeConfiguration<Membership>, IEntityTypeConfiguration<Participation>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations");
        builder.Property(static o => o.Id).ValueGeneratedNever();
        builder.Property(static o => o.Code).HasMaxLength(OrganizationUnit.CodeMaxLength).HasColumnType("citext");
        builder.Property(static o => o.Name).HasMaxLength(OrganizationUnit.NameMaxLength);
        builder.Property(static o => o.ShortName).HasMaxLength(OrganizationUnit.ShortNameMaxLength);
        builder.Property(static o => o.DatabaseName).HasMaxLength(Organization.DatabaseNameMaxLength);
        builder.HasIndex(static o => o.Code).IsUnique();
        builder.HasIndex(static o => o.DatabaseName).IsUnique();
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.ToTable("memberships");
        builder.HasKey(static m => new { m.UserId, m.OrganizationId });
        builder.HasIndex(static m => m.OrganizationId);
        builder.HasOne<User>().WithMany().HasForeignKey(static m => m.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Organization>().WithMany().HasForeignKey(static m => m.OrganizationId).OnDelete(DeleteBehavior.Cascade);
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Participation> builder)
    {
        builder.ToTable("participations");
        builder.HasKey(static p => new { p.UserId, p.OrganizationId });
        builder.HasIndex(static p => p.OrganizationId);
        builder.HasOne<User>().WithMany().HasForeignKey(static p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Organization>().WithMany().HasForeignKey(static p => p.OrganizationId).OnDelete(DeleteBehavior.Cascade);
    }
}
