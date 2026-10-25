using KOD.Application.Common.Files;
using KOD.Application.Common.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KOD.Web.Api;

/// <summary>
/// Base class of REST controllers: authenticated, JSON, with application results mapped to RFC 9457 problem details.
/// </summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Maps a result with a value: 200 with the value, or a problem.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result.</param>
    /// <returns>The action result.</returns>
    protected ActionResult<T> FromResult<T>(Result<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsSuccess ? Ok(result.Value) : ProblemFor(result.Error);
    }

    /// <summary>
    /// Maps a result without a value: 204, or a problem.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <returns>The action result.</returns>
    protected ActionResult FromResult(Result result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsSuccess ? NoContent() : ProblemFor(result.Error);
    }

    /// <summary>
    /// Maps a file result: the file stream (with range support), or a problem.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <returns>The action result.</returns>
    protected ActionResult FromFile(Result<DownloadableFile> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsSuccess
            ? File(result.Value.Content, result.Value.ContentType, result.Value.FileName, enableRangeProcessing: true)
            : ProblemFor(result.Error);
    }

    /// <summary>
    /// Converts an application error to a problem details response.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>The action result.</returns>
    protected ObjectResult ProblemFor(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var status = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status400BadRequest,
        };

        var problem = Problem(detail: error.Message, statusCode: status, title: error.Code);
        if (problem.Value is ProblemDetails details)
        {
            details.Extensions["code"] = error.Code;
        }

        return problem;
    }
}
