using KOD.Application.Result;

using Microsoft.AspNetCore.Mvc;

namespace KOD.WebApi.Extensions;

/// <summary>
/// Provides extension methods to convert <see cref="ApiResult{T}"/> to <see cref="IActionResult"/> for Web API responses.
/// </summary>
internal static class ApiResultExtensions
{
    #region Public methods

    /// <summary>
    /// Converts an <see cref="ApiResult{T}"/> to an <see cref="IActionResult"/> with proper status code, message, and data.
    /// </summary>
    /// <typeparam name="T">The type of data contained in the <see cref="ApiResult{T}"/>.</typeparam>
    /// <param name="result">The API result to convert.</param>
    /// <returns>An <see cref="IActionResult"/> representing the API result.</returns>
    public static IActionResult ToActionResult<T>(this ApiResult<T> result) 
        => new ObjectResult(new
                {
                    message = result.Message,
                    data = result.Data
                })
                {
                    StatusCode = result.StatusCode
                };

    #endregion
}
