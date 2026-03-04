using KOD.Application.Abstractions.Persitence.Transactions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KOD.Infrastructure.Implementations.Persistence.Transactions;

/// <summary>
/// Implements <see cref="ITransaction"/> for managing database transactions.
/// </summary>
internal sealed class Transaction : ITransaction
{
    #region Private fields

    /// <summary>
    /// The underlying database transaction.
    /// </summary>
    private readonly IDbContextTransaction _transaction;

    #endregion

    #region Public fields

    /// <inheritdoc />
    public DbContext DbContext { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="Transaction"/> class with the specified DbContext and database transaction.
    /// </summary>
    /// <param name="dbContext">The database context associated with the transaction.</param>
    /// <param name="transaction">The underlying database transaction.</param>
    public Transaction(DbContext dbContext, IDbContextTransaction transaction)
    {
        DbContext = dbContext;
        _transaction = transaction;
    }

    #endregion

    #region Public methods

    /// <inheritdoc />
    public async Task CommitAsync() => await _transaction.CommitAsync();

    /// <inheritdoc />
    public async Task RollbackAsync() => await _transaction.RollbackAsync();

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await _transaction.DisposeAsync();

    #endregion
}
