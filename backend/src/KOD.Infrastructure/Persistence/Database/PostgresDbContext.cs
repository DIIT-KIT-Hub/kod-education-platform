using Microsoft.EntityFrameworkCore;

namespace KOD.Infrastructure.Persistence.Database;

internal sealed class PostgresDbContext : DbContext
{
    public PostgresDbContext(DbContextOptions<PostgresDbContext> options) : base(options) { }
}
