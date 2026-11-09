using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ODC.Web.Infrastructure.Persistence.Configurations;

/// <summary>
/// Names the Identity claim, login and token tables in the database's snake_case style (<c>AspNetUserClaims</c> →
/// <c>user_claims</c>, unused: permissions are adjusted per organization; <c>AspNetUserLogins</c> → <c>user_logins</c>,
/// <c>AspNetUserTokens</c> → <c>user_tokens</c>).
/// </summary>
internal sealed class IdentityTablesConfiguration :
    IEntityTypeConfiguration<IdentityUserClaim<Guid>>,
    IEntityTypeConfiguration<IdentityUserLogin<Guid>>,
    IEntityTypeConfiguration<IdentityUserToken<Guid>>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> builder) => builder.ToTable("user_claims");

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> builder) => builder.ToTable("user_logins");

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IdentityUserToken<Guid>> builder) => builder.ToTable("user_tokens");
}
