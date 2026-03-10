namespace KOD.Application.Abstractions.Persitence.Transactions;

/// <summary>
/// Defines a manager for handling database transactions.
/// </summary>
public interface ITransactionManager
{
    #region Public methods

    /// <summary>
    /// Begins a new database transaction asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the started <see cref="ITransaction"/>.</returns>
    Task<ITransaction> BeginTransactionAsync();

    #endregion
}
