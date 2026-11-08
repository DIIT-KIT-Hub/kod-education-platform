using Microsoft.AspNetCore.Http.HttpResults;
using ODC.Web.Shared.Files;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Endpoints;

/// <summary>
/// Turns handler results into HTTP answers: success as 200/204/file, an error as RFC 9457 problem details with the
/// stable <c>code</c> the client switches on.
/// </summary>
public static class ResultHttpExtensions
{
    /// <summary>
    /// Builds the problem details answer of an error (status from its type, <c>title</c> and <c>code</c> = its code).
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>The problem answer.</returns>
    public static ProblemHttpResult ToProblem(this Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var status = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.TooManyRequests => StatusCodes.Status429TooManyRequests,
            ErrorType.Unavailable => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status400BadRequest,
        };

        return TypedResults.Problem(
            detail: error.Message,
            statusCode: status,
            title: error.Code,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }

    /// <summary>
    /// 200 with the value, or the problem.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result.</param>
    /// <returns>The answer.</returns>
    public static Results<Ok<T>, ProblemHttpResult> ToOk<T>(this Result<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    /// <summary>
    /// 201 with the new identifier and its address, or the problem.
    /// </summary>
    /// <param name="result">The result with the new identifier.</param>
    /// <param name="location">Builds the address of the created record from its identifier.</param>
    /// <returns>The answer.</returns>
    public static Results<Created<CreatedId>, ProblemHttpResult> ToCreated(this Result<Guid> result, Func<Guid, string> location)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(location);
        return result.IsSuccess ? TypedResults.Created(location(result.Value), new CreatedId(result.Value)) : result.Error.ToProblem();
    }

    /// <summary>
    /// 204, or the problem.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <returns>The answer.</returns>
    public static Results<NoContent, ProblemHttpResult> ToNoContent(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();
    }

    /// <summary>
    /// The file as a download (range requests allowed), or the problem.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <returns>The answer.</returns>
    public static Results<FileStreamHttpResult, ProblemHttpResult> ToFile(this Result<DownloadableFile> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsSuccess
            ? TypedResults.File(result.Value.Content, result.Value.ContentType, result.Value.FileName, enableRangeProcessing: true)
            : result.Error.ToProblem();
    }
}
