using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Faster.Modulith.Contracts;

/// <summary>
/// Defines the category of the error to drive HTTP status codes.
/// </summary>
public enum ErrorType
{
    Failure,        // 400 Bad Request (Default)
    Validation,     // 400 Bad Request
    NotFound,       // 404 Not Found
    Conflict,       // 409 Conflict
    Unauthorized,   // 401 Unauthorized
    Forbidden       // 403 Forbidden
}

/// <summary>
/// Represents the outcome of an operation without a value.
/// </summary>
public readonly struct Result : IEquatable<Result>
{
    public bool IsSuccess { get; }
    public string Error { get; }
    public ErrorType ErrorType { get; }
    public bool IsFailure => !IsSuccess;

    private Result(bool isSuccess, string error, ErrorType errorType)
    {
        IsSuccess = isSuccess;
        Error = error ?? string.Empty;
        ErrorType = errorType;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success => new Result(true, string.Empty, ErrorType.Failure);

    /// <summary>
    /// Creates a failed result (defaults to 400 Bad Request).
    /// </summary>
    public static Result Failure(string error, ErrorType errorType = ErrorType.Failure)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("Error message must not be empty", nameof(error));
        return new Result(false, error, errorType);
    }

    // --- Status Code Factories ---
    public static Result NotFound(string error = "Resource not found.") => Failure(error, ErrorType.NotFound);
    public static Result Conflict(string error = "Resource conflict occurred.") => Failure(error, ErrorType.Conflict);
    public static Result Unauthorized(string error = "Unauthorized access.") => Failure(error, ErrorType.Unauthorized);
    public static Result Forbidden(string error = "Access forbidden.") => Failure(error, ErrorType.Forbidden);
    public static Result Validation(string error) => Failure(error, ErrorType.Validation);

    /// <summary>
    /// Executes an action based on the result outcome.
    /// </summary>
    public void Match(Action success, Action<string> failure)
    {
        if (IsSuccess) success();
        else failure(Error);
    }

    /// <summary>
    /// Returns a value based on the result outcome.
    /// </summary>
    public TResult Match<TResult>(Func<TResult> success, Func<string, TResult> failure)
    {
        return IsSuccess ? success() : failure(Error);
    }

    /// <summary>
    /// Converts a non-generic Result to a generic Result&lt;T&gt;.
    /// </summary>
    public Result<T> To<T>(T value = default) =>
        IsSuccess ? Result<T>.Success(value) : Result<T>.Failure(Error, ErrorType);

    /// <summary>
    /// Executes the next function if the current result is successful.
    /// </summary>
    public Result Bind(Func<Result> next)
    {
        return IsSuccess ? next() : Failure(Error, ErrorType);
    }

    /// <summary>
    /// Wraps a function that might throw an exception into a Result.
    /// </summary>
    public static Result Try(Action action, string errorMessagePrefix = "Operation failed")
    {
        try
        {
            action();
            return Success;
        }
        catch (Exception ex)
        {
            return Failure($"{errorMessagePrefix}: {ex.Message}");
        }
    }

    /// <summary>
    /// Wraps a function returning T that might throw an exception into a Result&lt;T&gt;.
    /// </summary>
    public static Result<T> Try<T>(Func<T> action, string errorMessagePrefix = "Operation failed")
    {
        try
        {
            return Result<T>.Success(action());
        }
        catch (Exception ex)
        {
            return Result<T>.Failure($"{errorMessagePrefix}: {ex.Message}");
        }
    }

    /// <summary>
    /// Combines multiple results. Returns Success if all are successful, otherwise Failure.
    /// </summary>
    public static Result Combine(params Result[] results)
    {
        var errors = new List<string>();
        foreach (var result in results)
        {
            if (result.IsFailure) errors.Add(result.Error);
        }

        return errors.Count > 0
            ? Failure(string.Join(", ", errors))
            : Success;
    }

    public override string ToString() => IsSuccess ? "Success" : $"Failure ({ErrorType}): {Error}";

    // Equality
    public override bool Equals(object obj) => obj is Result other && Equals(other);
    public bool Equals(Result other) => IsSuccess == other.IsSuccess && Error == other.Error && ErrorType == other.ErrorType;
    public static bool operator ==(Result left, Result right) => left.Equals(right);
    public static bool operator !=(Result left, Result right) => !left.Equals(right);
}

/// <summary>
/// Represents the outcome of an operation with a value.
/// </summary>
public readonly struct Result<T> : IEquatable<Result<T>>
{
    public bool IsSuccess { get; }
    public string Error { get; }
    public ErrorType ErrorType { get; }
    public T Value { get; }
    public bool IsFailure => !IsSuccess;

    private Result(bool isSuccess, T value, string error, ErrorType errorType)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error ?? string.Empty;
        ErrorType = errorType;
    }

    public static Result<T> Success(T value) => new Result<T>(true, value, string.Empty, ErrorType.Failure);

    public static Result<T> Failure(string error, ErrorType errorType = ErrorType.Failure)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("Error message must not be empty", nameof(error));
        return new Result<T>(false, default!, error, errorType);
    }

    // --- Status Code Factories ---
    public static Result<T> NotFound(string error = "Resource not found.") => Failure(error, ErrorType.NotFound);
    public static Result<T> Conflict(string error = "Resource conflict occurred.") => Failure(error, ErrorType.Conflict);
    public static Result<T> Unauthorized(string error = "Unauthorized access.") => Failure(error, ErrorType.Unauthorized);
    public static Result<T> Forbidden(string error = "Access forbidden.") => Failure(error, ErrorType.Forbidden);
    public static Result<T> Validation(string error) => Failure(error, ErrorType.Validation);

    /// <summary>
    /// Implicitly converts a value to a Success Result.
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);

    public void Match(Action<T> success, Action<string> failure)
    {
        if (IsSuccess) success(Value);
        else failure(Error);
    }

    public TResult Match<TResult>(Func<T, TResult> success, Func<string, TResult> failure)
    {
        return IsSuccess ? success(Value) : failure(Error);
    }

    public Result ToResult() => IsSuccess ? Result.Success : Result.Failure(Error, ErrorType);

    /// <summary>
    /// Transforms the inner value if the result is successful.
    /// </summary>
    public Result<TOutput> Map<TOutput>(Func<T, TOutput> mapper)
    {
        return IsSuccess ? Result<TOutput>.Success(mapper(Value)) : Result<TOutput>.Failure(Error, ErrorType);
    }

    /// <summary>
    /// Chains another operation that returns a Result.
    /// </summary>
    public Result<TOutput> Bind<TOutput>(Func<T, Result<TOutput>> next)
    {
        return IsSuccess ? next(Value) : Result<TOutput>.Failure(Error, ErrorType);
    }

    /// <summary>
    /// Ensures the value satisfies a predicate, otherwise returns Failure.
    /// </summary>
    public Result<T> Ensure(Func<T, bool> predicate, string errorMessage)
    {
        if (IsFailure) return this;
        return predicate(Value) ? this : Failure(errorMessage);
    }

    public override string ToString() => IsSuccess ? $"Success: {Value}" : $"Failure ({ErrorType}): {Error}";

    // Equality
    public override bool Equals(object obj) => obj is Result<T> other && Equals(other);
    public bool Equals(Result<T> other)
    {
        if (IsSuccess != other.IsSuccess) return false;
        if (!IsSuccess) return Error == other.Error && ErrorType == other.ErrorType;
        return EqualityComparer<T>.Default.Equals(Value, other.Value);
    }
    public static bool operator ==(Result<T> left, Result<T> right) => left.Equals(right);
    public static bool operator !=(Result<T> left, Result<T> right) => !left.Equals(right);
}

/// <summary>
/// Extensions for Async flows, Side-effects (Tap), and Task chaining.
/// </summary>
public static class ResultExtensions
{
    // --- TAP (Side Effects) ---

    public static Result<T> Tap<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess) action(result.Value);
        return result;
    }

    public static async Task<Result<T>> TapAsync<T>(this Task<Result<T>> resultTask, Func<T, Task> action)
    {
        var result = await resultTask;
        if (result.IsSuccess) await action(result.Value);
        return result;
    }

    // --- ASYNC CHAINING ---

    public static async Task<Result<U>> MapAsync<T, U>(this Task<Result<T>> resultTask, Func<T, Task<U>> mapper)
    {
        var result = await resultTask;
        // Propagate ErrorType
        if (result.IsFailure) return Result<U>.Failure(result.Error, result.ErrorType);

        var newValue = await mapper(result.Value);
        return Result<U>.Success(newValue);
    }

    public static async Task<Result<U>> BindAsync<T, U>(this Task<Result<T>> resultTask, Func<T, Task<Result<U>>> next)
    {
        var result = await resultTask;
        // Propagate ErrorType
        if (result.IsFailure) return Result<U>.Failure(result.Error, result.ErrorType);

        return await next(result.Value);
    }

    public static async Task<TOutput> MatchAsync<T, TOutput>(
        this Task<Result<T>> resultTask,
        Func<T, TOutput> onSuccess,
        Func<string, TOutput> onFailure)
    {
        var result = await resultTask;
        return result.Match(onSuccess, onFailure);
    }
}