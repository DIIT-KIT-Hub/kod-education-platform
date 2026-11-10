using Microsoft.EntityFrameworkCore;
using ODC.Web.Features.News.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Infrastructure.Tenancy;
using ODC.Web.Shared.Domain.Tenancy;

namespace ODC.Web.Features.News;

/// <summary>
/// The news of the whole cabinet across organizations: keeps their index in the master database
/// (<see cref="GeneralArticle"/>) and reads the ones other organizations hold.
/// </summary>
/// <param name="master">The master database (the index).</param>
/// <param name="organization">The organization of the request.</param>
/// <param name="registry">The registry of organizations.</param>
/// <param name="scopes">Runs reads in other organizations.</param>
internal sealed class GeneralNews(MasterDbContext master, OrganizationContext organization, OrganizationRegistry registry, OrganizationScopes scopes)
{
    /// <summary>
    /// Rebuilds the index from the databases of every organization (at the start).
    /// </summary>
    /// <param name="master">The master database.</param>
    /// <param name="organizations">The organizations.</param>
    /// <param name="scopes">Runs reads in an organization.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    public static async Task RebuildAsync(MasterDbContext master, IReadOnlyList<Organization> organizations, OrganizationScopes scopes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(master);
        ArgumentNullException.ThrowIfNull(organizations);
        ArgumentNullException.ThrowIfNull(scopes);
        var wanted = new Dictionary<Guid, GeneralArticle>();
        foreach (var other in organizations)
        {
            var published = await scopes.RunAsync(other, services => services.GetRequiredService<OrganizationDbContext>().NewsArticles.AsNoTracking()
                .Where(static a => a.OrganizationId == null && a.PublishedAt != null)
                .Select(static a => new { a.Id, PublishedAt = a.PublishedAt!.Value })
                .ToListAsync(cancellationToken));
            foreach (var article in published)
            {
                wanted[article.Id] = new GeneralArticle(article.Id, other.Id, article.PublishedAt);
            }
        }

        var existing = await master.GeneralArticles.ToListAsync(cancellationToken);
        master.GeneralArticles.RemoveRange(existing.Where(e => !wanted.ContainsKey(e.ArticleId) || wanted[e.ArticleId].OrganizationId != e.OrganizationId));
        foreach (var entry in existing.Where(e => wanted.TryGetValue(e.ArticleId, out var w) && w.OrganizationId == e.OrganizationId))
        {
            entry.Republish(wanted[entry.ArticleId].PublishedAt);
        }

        var kept = existing.Where(e => wanted.TryGetValue(e.ArticleId, out var w) && w.OrganizationId == e.OrganizationId).Select(static e => e.ArticleId).ToHashSet();
        master.GeneralArticles.AddRange(wanted.Values.Where(w => !kept.Contains(w.ArticleId)));
        await master.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Brings the index in line with an article after it was saved or deleted.
    /// </summary>
    /// <param name="article">The article.</param>
    /// <param name="deleted">Whether it was deleted.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    public async Task SyncAsync(NewsArticle article, bool deleted, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(article);
        var here = organization.Require().Id;
        var entry = await master.GeneralArticles.FirstOrDefaultAsync(a => a.ArticleId == article.Id, cancellationToken);
        if (!deleted && article.OrganizationId is null && article.PublishedAt is { } published)
        {
            if (entry is null)
            {
                master.GeneralArticles.Add(new GeneralArticle(article.Id, here, published));
            }
            else
            {
                entry.Republish(published);
            }
        }
        else if (entry is not null)
        {
            master.GeneralArticles.Remove(entry);
        }
        else
        {
            return;
        }

        await master.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Reads from every other organization that holds news of the whole cabinet.
    /// </summary>
    /// <typeparam name="T">What one organization gives.</typeparam>
    /// <param name="read">The read, given the organization's database and the identifiers of its general articles.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The results with their organizations.</returns>
    public async Task<List<Gathered<T>>> FromOthersAsync<T>(Func<OrganizationDbContext, List<Guid>, Task<T>> read, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(read);
        var here = organization.Current?.Id;
        var entries = await master.GeneralArticles.AsNoTracking()
            .Where(a => a.OrganizationId != here)
            .Select(static a => new { a.ArticleId, a.OrganizationId })
            .ToListAsync(cancellationToken);
        var result = new List<Gathered<T>>();
        foreach (var group in entries.GroupBy(static e => e.OrganizationId))
        {
            if (await registry.FindAsync(group.Key, cancellationToken) is not { IsActive: true } other)
            {
                continue;
            }

            var ids = group.Select(static e => e.ArticleId).ToList();
            result.Add(new Gathered<T>(other, await scopes.RunAsync(other, services => read(services.GetRequiredService<OrganizationDbContext>(), ids))));
        }

        return result;
    }
}
