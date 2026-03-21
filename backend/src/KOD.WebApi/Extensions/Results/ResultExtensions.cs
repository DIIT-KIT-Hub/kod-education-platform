using KOD.Application.Results;
using KOD.Domain.Enums.Errors;

using Microsoft.AspNetCore.Mvc;

namespace KOD.WebApi.Extensions.Results;

internal static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return new NoContentResult();
        }

        return MapFailure(result.Error);
    }

    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        return MapFailure(result.Error);
    }

    private static ObjectResult MapFailure(Error error) 
        => error.ErrorType switch
        {
            ErrorType.Validation => new BadRequestObjectResult(error),
            ErrorType.NotFound => new NotFoundObjectResult(error),
            ErrorType.Unauthorized => new UnauthorizedObjectResult(error),
            ErrorType.Forbidden => new ObjectResult(error) { StatusCode = 403 },
            ErrorType.Conflict => new ConflictObjectResult(error),
            _ => new ObjectResult(error) { StatusCode = 500 }
        };
}
