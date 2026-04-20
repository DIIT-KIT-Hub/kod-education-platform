using KOD.Application.Results;
using KOD.Domain.Enums.Errors;

using Microsoft.AspNetCore.Mvc;

namespace KOD.WebApi.Extensions.Results;

/// <summary>
/// Extensions for converting <see cref="Result"/> objects to HTTP responses.
/// </summary>
internal static class ResultExtensions
{
    #region Public methods

    /// <summary>
    /// Converts a generic result to an <see cref="IActionResult"/>.
    /// </summary>
    /// <typeparam name="T">Type of the result value.</typeparam>
    /// <param name="result">The result instance.</param>
    /// <returns>200 OK with value if success; otherwise mapped error response.</returns>
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        return MapFailure(result.Error);
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Maps an error to the corresponding HTTP response.
    /// </summary>
    /// <param name="error">The error to map.</param>
    /// <returns>An <see cref="ObjectResult"/> with the appropriate HTTP status code.</returns>
    private static ObjectResult MapFailure(Error error) 
        => error.ErrorType switch
        {
            ErrorType.Validation => new BadRequestObjectResult(error),
            ErrorType.NotFound => new NotFoundObjectResult(error),
            ErrorType.Gone => new ObjectResult(error) { StatusCode = 410 },
            ErrorType.Unauthorized => new UnauthorizedObjectResult(error),
            ErrorType.Forbidden => new ObjectResult(error) { StatusCode = 403 },
            ErrorType.Conflict => new ConflictObjectResult(error),
            _ => new ObjectResult(error) { StatusCode = 500 }
        };

    #endregion
}
