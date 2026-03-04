using Microsoft.EntityFrameworkCore;

namespace KOD.Application.Abstractions.Persitence.Transactions;

/// <summary>
/// Represents a database transaction that can be committed or rolled back.
/// </summary>
public interface ITransaction : IAsyncDisposable
{
    #region Public fields

    /// <summary>
    /// Gets the underlying <see cref="DbContext"/> associated with the transaction.
    /// </summary>
    DbContext DbContext { get; }

    #endregion

    #region Public methods

    /// <summary>
    /// Commits the transaction asynchronously.
    /// </summary>
    Task CommitAsync();

    /// <summary>
    /// Rolls back the transaction asynchronously.
    /// </summary>
    Task RollbackAsync();

    #endregion
}
