using KOD.Domain.Enums.Errors;

namespace KOD.Application.Results;

public static class Errors
{
    public static Error Validation(string message) =>
        new(ErrorType.Validation, "Validation.Error", message);

    public static Error NotFound(string entity) =>
        new(ErrorType.NotFound, $"{entity}.NotFound", $"{entity} was not found");

    public static Error Unauthorized(string message = "Unauthorized") =>
        new(ErrorType.Unauthorized, "Auth.Unauthorized", message);

    public static Error Forbidden(string message = "Forbidden") =>
        new(ErrorType.Forbidden, "Auth.Forbidden", message);

    public static Error Conflict(string message) =>
        new(ErrorType.Conflict, "Conflict.Error", message);

    public static Error Gone(string message) =>
        new(ErrorType.Gone, "Gone.Error", message);

    public static Error Failure(string message) =>
        new(ErrorType.Failure, "Failure.Error", message);
}
