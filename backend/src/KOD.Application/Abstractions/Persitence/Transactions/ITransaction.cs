namespace KOD.Application.Abstractions.Persitence.Transactions;

/// <summary>
/// Represents a database transaction that can be committed or rolled back.
/// </summary>
public interface ITransaction : IAsyncDisposable
{
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
