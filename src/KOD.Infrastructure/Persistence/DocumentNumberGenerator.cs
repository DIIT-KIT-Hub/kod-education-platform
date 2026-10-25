using System.Globalization;
using KOD.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace KOD.Infrastructure.Persistence;

/// <summary>
/// Issues registration numbers from a PostgreSQL sequence: <c>{year}/{number:000000}</c>.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="timeProvider">The clock.</param>
internal sealed class DocumentNumberGenerator(ApplicationDbContext db, TimeProvider timeProvider) : IDocumentNumberGenerator
{
    /// <inheritdoc />
    public async Task<string> NextAsync(CancellationToken cancellationToken)
    {
        var next = await db.Database
            .SqlQueryRaw<long>($"SELECT nextval('{ApplicationDbContext.DocumentNumberSequence}') AS \"Value\"")
            .SingleAsync(cancellationToken);

        return string.Create(CultureInfo.InvariantCulture, $"{timeProvider.GetUtcNow().Year}/{next:D6}");
    }
}
