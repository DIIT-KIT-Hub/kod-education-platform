using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ODC.Domain.Users;
using ODC.Infrastructure.Identity.Activation;

namespace ODC.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="ActivationChallenge"/>.
/// </summary>
internal sealed class ActivationChallengeConfiguration : IEntityTypeConfiguration<ActivationChallenge>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ActivationChallenge> builder)
    {
        builder.ToTable("activation_challenges");
        builder.Property(static c => c.EmailHash).HasMaxLength(ActivationChallenge.EmailHashLength).IsFixedLength();
        builder.Property(static c => c.ProtectedCode).HasMaxLength(512);

        // PostgreSQL "xmin": parallel guesses of one code are all counted, and only one can verify it.
        builder.Property<uint>("Version").IsRowVersion();
        builder.HasIndex(static c => new { c.EmailHash, c.CreatedAt });
        builder.HasIndex(static c => c.CreatedAt);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(static c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
