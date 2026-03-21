using KOD.Domain.Enums.Errors;

namespace KOD.Application.Results;

public sealed record Error
{
    public ErrorType ErrorType { get; init; }
    public string Code { get; init; }
    public string Message { get; init; }

    public static readonly Error None = new(ErrorType.None, string.Empty, string.Empty);

    public Error(ErrorType errorType, string code, string message)
    {
        ErrorType = errorType;
        Code = code;
        Message = message;
    }
}
