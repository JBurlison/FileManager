namespace WebFileExplorer.Shared.Models;

public class Result<T> : Result
{
    public T? Value { get; set; }

    public static Result<T> Success(T value) => new Result<T> { IsSuccess = true, Value = value };
    public static new Result<T> Failure(string errorMessage) => new Result<T> { IsSuccess = false, ErrorMessage = errorMessage };
}

public class Result
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public int SuccessfulCount { get; set; }
    public int FailedCount { get; set; }
    public Dictionary<string, string> Errors { get; set; } = new();

    public static Result Success() => new Result { IsSuccess = true };
    public static Result Failure(string errorMessage) => new Result { IsSuccess = false, ErrorMessage = errorMessage };
}

