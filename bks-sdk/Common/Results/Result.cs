namespace bks.sdk.Common.Results;


public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public bool IsFailure => !IsSuccess;

    protected Result(bool isSuccess, string? error)
    {
        if (isSuccess && error != null)
            throw new InvalidOperationException("Successful result cannot have an error");

        if (!isSuccess && error == null)
            throw new InvalidOperationException("Failed result must have an error");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);

    public static implicit operator Result(string error) => Failure(error);

    public override string ToString()
    {
        return IsSuccess ? "Success" : $"Failure: {Error}";
    }
}

public class Result<T> : Result
{
    public T? Value { get; }

    protected internal Result(bool isSuccess, T? value, string? error) : base(isSuccess, error)
    {
        if (isSuccess && value == null && !typeof(T).IsClass)
            throw new InvalidOperationException("Successful result must have a value for value types");

        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static new Result<T> Failure(string error) => new(false, default, error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(string error) => Failure(error);

    public override string ToString()
    {
        return IsSuccess ? $"Success: {Value}" : $"Failure: {Error}";
    }
}
