namespace SmartTaxi.Application.Common;

public enum ErrorType
{
    Validation,
    Conflict,
    Unauthorized
}

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public ErrorType? ErrorType { get; }

    protected Result(bool isSuccess, string? error, ErrorType? errorType)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorType = errorType;
    }

    public static Result Success() => new(true, null, null);

    public static Result Failure(string error, ErrorType errorType) => new(false, error, errorType);
}

public class Result<T> : Result
{
    public T? Value { get; }

    protected Result(T? value, bool isSuccess, string? error, ErrorType? errorType)
        : base(isSuccess, error, errorType)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(value, true, null, null);

    public static new Result<T> Failure(string error, ErrorType errorType) => new(default, false, error, errorType);
}
