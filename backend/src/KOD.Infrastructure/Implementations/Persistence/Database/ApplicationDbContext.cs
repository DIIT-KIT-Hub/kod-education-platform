using KOD.Domain.Entities.Auth;
using KOD.Domain.Entities.Otp;
using KOD.Domain.Entities.Users;
using KOD.Infrastructure.Implementations.Persistence.Configurations;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KOD.Infrastructure.Implementations.Persistence.Database;

/// <summary>
/// Represents the application's database context.
/// </summary>
internal sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    #region Public fields

    /// <summary>
    /// Gets or sets the OTP codes stored in the database.
    /// </summary>
    public DbSet<OtpCode> OtpCodes { get; set; }

    /// <summary>
    /// Gets or sets the refresh tokens stored in the database.
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class with the specified options.
    /// </summary>
    /// <param name="options">The options for configuring the database context.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    #endregion

    #region Public methods

    /// <summary>
    /// Configures the schema needed for the context including.
    /// </summary>
    /// <param name="builder">The model builder used to configure entities.</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationUserConfiguration).Assembly);
    }

    #endregion
}
