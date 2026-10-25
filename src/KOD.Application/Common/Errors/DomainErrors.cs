using KOD.Application.Common.Results;
using KOD.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace KOD.Application.Common.Errors;

/// <summary>
/// Converts domain and persistence exceptions into <see cref="Error"/> values.
/// </summary>
public static class DomainErrors
{
    /// <summary>
    /// Runs a mutation and maps expected exceptions to failed results.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="operation">The operation.</param>
    /// <returns>The operation result or a mapped error.</returns>
    public static async Task<Result<T>> GuardAsync<T>(Func<Task<Result<T>>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        try
        {
            return await operation();
        }
        catch (DomainException exception)
        {
            return Error.Validation(exception.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Error.Conflict("Дані щойно змінив інший користувач. Оновіть сторінку та спробуйте ще раз.");
        }
    }

    /// <summary>
    /// Runs a mutation without a value and maps expected exceptions to failed results.
    /// </summary>
    /// <param name="operation">The operation.</param>
    /// <returns>The operation result or a mapped error.</returns>
    public static async Task<Result> GuardAsync(Func<Task<Result>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        try
        {
            return await operation();
        }
        catch (DomainException exception)
        {
            return Error.Validation(exception.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Error.Conflict("Дані щойно змінив інший користувач. Оновіть сторінку та спробуйте ще раз.");
        }
    }
}
