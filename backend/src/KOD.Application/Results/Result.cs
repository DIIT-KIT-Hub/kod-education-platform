namespace KOD.Application.Results;

public class Result
{
    public bool IsSuccess { get; }
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("Success result cannot have error");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("Failure result must have error");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);
}


public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(T? value, bool isSuccess, Error error) : base(isSuccess, error) => Value = value;

    public static Result<T> Success(T value) =>
        new(value, true, Error.None);

    public static new Result<T> Failure(Error error) =>
        new(default, false, error);
}
