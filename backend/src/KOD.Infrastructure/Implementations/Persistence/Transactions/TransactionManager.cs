using KOD.Application.Abstractions.Persitence.Transactions;
using KOD.Infrastructure.Implementations.Persistence.Database;

namespace KOD.Infrastructure.Implementations.Persistence.Transactions;

/// <summary>
/// Implements <see cref="ITransactionManager"/> for creating and managing database transactions.
/// </summary>
internal sealed class TransactionManager : ITransactionManager
{
    #region Private fields

    /// <summary>
    /// The database context used to access authentication-related entities.
    /// </summary>
    private readonly ApplicationDbContext _dbContext;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionManager"/> class with the specified database context.
    /// </summary>
    /// <param name="dbContext">The application's database context.</param>
    public TransactionManager(ApplicationDbContext dbContext) => _dbContext = dbContext;

    #endregion

    #region Public methods

    /// <inheritdoc />
    public async Task<ITransaction> BeginTransactionAsync()
    {
        var transaction = await _dbContext.Database.BeginTransactionAsync();
        return new Transaction(_dbContext, transaction);
    }

    #endregion
}
