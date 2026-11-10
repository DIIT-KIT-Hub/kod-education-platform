using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ODC.Web.Shared.Domain.Users;

namespace ODC.Web.Infrastructure.Persistence;

/// <summary>
/// Keeps the organizations' copies of the accounts (<see cref="Member"/>) up to date: after the master database saved new
/// or changed accounts, their profiles go to the databases of the organizations they work in (<see cref="Tenancy.MemberCopies"/>),
/// each in a scope of its own (so the request's own pending changes there are not saved early). Inside a transaction the
/// copies wait for the commit and are dropped on a rollback. Catches every way an account changes, the Identity managers
/// included.
/// </summary>
/// <remarks>
/// A crash between the two saves leaves an old copy until the account changes again; the seeding repairs the copies of
/// the members of every organization at start.
/// </remarks>
/// <param name="copy">Writes profiles to the organizations' databases.</param>
internal sealed class MemberSync(Func<IReadOnlyCollection<MemberProfile>, CancellationToken, Task> copy) : SaveChangesInterceptor, IDbTransactionInterceptor
{
    /// <summary>
    /// The profiles of the accounts the current save adds or changes.
    /// </summary>
    private List<MemberProfile> _saving = [];

    /// <summary>
    /// The profiles saved inside a transaction that is not committed yet.
    /// </summary>
    private List<MemberProfile> _waiting = [];

    /// <summary>
    /// Copies the given profiles into the organization's database.
    /// </summary>
    /// <param name="organization">The organization's database.</param>
    /// <param name="profiles">The profiles.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    public static async Task CopyAsync(OrganizationDbContext organization, IReadOnlyCollection<MemberProfile> profiles, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(organization);
        ArgumentNullException.ThrowIfNull(profiles);
        if (profiles.Count == 0)
        {
            return;
        }

        // The latest profile of each account wins.
        var latest = profiles.GroupBy(static p => p.Id).Select(static g => g.Last()).ToList();
        var ids = latest.Select(static p => p.Id).ToList();
        var members = await organization.Members.Where(m => ids.Contains(m.Id)).ToDictionaryAsync(static m => m.Id, cancellationToken);
        foreach (var profile in latest)
        {
            if (members.TryGetValue(profile.Id, out var member))
            {
                member.Copy(profile);
            }
            else
            {
                organization.Members.Add(new Member(profile));
            }
        }

        await organization.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        Collect(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        Collect(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        if (Saved(eventData.Context))
        {
            FlushAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        return base.SavedChanges(eventData, result);
    }

    /// <inheritdoc />
    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        if (Saved(eventData.Context))
        {
            await FlushAsync(cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        _saving = [];
        base.SaveChangesFailed(eventData);
    }

    /// <inheritdoc />
    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        _saving = [];
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    /// <inheritdoc />
    public void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
        => FlushAsync(CancellationToken.None).GetAwaiter().GetResult();

    /// <inheritdoc />
    public Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        => FlushAsync(cancellationToken);

    /// <inheritdoc />
    public void TransactionRolledBack(DbTransaction transaction, TransactionEndEventData eventData) => _waiting = [];

    /// <inheritdoc />
    public Task TransactionRolledBackAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        _waiting = [];
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void TransactionFailed(DbTransaction transaction, TransactionErrorEventData eventData) => _waiting = [];

    /// <inheritdoc />
    public Task TransactionFailedAsync(DbTransaction transaction, TransactionErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        _waiting = [];
        return Task.CompletedTask;
    }

    /// <summary>
    /// Takes the profiles of the accounts the save adds or changes.
    /// </summary>
    /// <param name="context">The saving context.</param>
    private void Collect(DbContext? context)
    {
        if (context is null)
        {
            _saving = [];
            return;
        }

        _saving =
        [
            .. context.ChangeTracker.Entries<User>()
                .Where(static e => e.State is EntityState.Added or EntityState.Modified)
                .Select(static e => MemberProfile.Of(e.Entity)),
        ];
    }

    /// <summary>
    /// Moves the saved profiles to the waiting list.
    /// </summary>
    /// <param name="context">The context that saved.</param>
    /// <returns>Whether they can be copied now (no transaction is open).</returns>
    private bool Saved(DbContext? context)
    {
        _waiting.AddRange(_saving);
        _saving = [];
        return context?.Database.CurrentTransaction is null;
    }

    /// <summary>
    /// Writes the waiting profiles to the organization's database.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    private async Task FlushAsync(CancellationToken cancellationToken)
    {
        var waiting = _waiting;
        _waiting = [];
        if (waiting.Count == 0)
        {
            return;
        }

        await copy(waiting, cancellationToken);
    }
}
