using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Faster.Modulith.Contracts;

/// <summary>
/// Represents the outcome of an operation without a value.
/// </summary>
public readonly struct Result : IEquatable<Result>
{
    public bool IsSuccess { get; }
    public string Error { get; }
    public bool IsFailure => !IsSuccess;

    private Result(bool isSuccess, string error)
    {
        IsSuccess = isSuccess;
        Error = error ?? string.Empty;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success => new Result(true, string.Empty);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static Result Failure(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("Error message must not be empty", nameof(error));
        return new Result(false, error);
    }

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
    /// <example>
    /// <code>
    /// string msg = result.Match(
    ///     () => "Operation OK",
    ///     err => $"Error: {err}"
    /// );
    /// </code>
    /// </example>
    public TResult Match<TResult>(Func<TResult> success, Func<string, TResult> failure)
    {
        return IsSuccess ? success() : failure(Error);
    }

    /// <summary>
    /// Converts a non-generic Result to a generic Result&lt;T&gt;.
    /// </summary>
    public Result<T> To<T>(T value = default) =>
        IsSuccess ? Result<T>.Success(value) : Result<T>.Failure(Error);

    /// <summary>
    /// Executes the next function if the current result is successful.
    /// </summary>
    public Result Bind(Func<Result> next)
    {
        return IsSuccess ? next() : Failure(Error);
    }

    /// <summary>
    /// Wraps a function that might throw an exception into a Result.
    /// </summary>
    /// <example>
    /// <code>
    /// var res = Result.Try(() => File.Delete("temp.txt"));
    /// </code>
    /// </example>
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
    /// Combines multiple results. Returns Success if all are successful, otherwise Failure with all errors joined.
    /// </summary>
    /// <example>
    /// <code>
    /// var validation = Result.Combine(ValidateName(n), ValidateEmail(e));
    /// if (validation.IsFailure) return validation;
    /// </code>
    /// </example>
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

    public override string ToString() => IsSuccess ? "Success" : $"Failure: {Error}";

    // Equality
    public override bool Equals(object obj) => obj is Result other && Equals(other);
    public bool Equals(Result other) => IsSuccess == other.IsSuccess && Error == other.Error;
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
    public T Value { get; }
    public bool IsFailure => !IsSuccess;

    private Result(bool isSuccess, T value, string error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error ?? string.Empty;
    }

    public static Result<T> Success(T value) => new Result<T>(true, value, string.Empty);

    public static Result<T> Failure(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("Error message must not be empty", nameof(error));
        return new Result<T>(false, default!, error);
    }

    /// <summary>
    /// Implicitly converts a value to a Success Result.
    /// </summary>
    /// <example>
    /// <code>
    /// Result&lt;int&gt; GetId() => 5; // Automatically wrapped
    /// </code>
    /// </example>
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

    public Result ToResult() => IsSuccess ? Result.Success : Result.Failure(Error);

    /// <summary>
    /// Transforms the inner value if the result is successful.
    /// </summary>
    /// <example>
    /// <code>
    /// result.Map(x => x.ToString());
    /// </code>
    /// </example>
    public Result<TOutput> Map<TOutput>(Func<T, TOutput> mapper)
    {
        return IsSuccess ? Result<TOutput>.Success(mapper(Value)) : Result<TOutput>.Failure(Error);
    }

    /// <summary>
    /// Chains another operation that returns a Result.
    /// </summary>
    /// <example>
    /// <code>
    /// GetUser(id).Bind(user => GetOrders(user.Id));
    /// </code>
    /// </example>
    public Result<TOutput> Bind<TOutput>(Func<T, Result<TOutput>> next)
    {
        return IsSuccess ? next(Value) : Result<TOutput>.Failure(Error);
    }

    /// <summary>
    /// Ensures the value satisfies a predicate, otherwise returns Failure.
    /// </summary>
    public Result<T> Ensure(Func<T, bool> predicate, string errorMessage)
    {
        if (IsFailure) return this;
        return predicate(Value) ? this : Failure(errorMessage);
    }

    public override string ToString() => IsSuccess ? $"Success: {Value}" : $"Failure: {Error}";

    // Equality
    public override bool Equals(object obj) => obj is Result<T> other && Equals(other);
    public bool Equals(Result<T> other)
    {
        if (IsSuccess != other.IsSuccess) return false;
        if (!IsSuccess) return Error == other.Error;
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

    /// <summary>
    /// Executes an action (e.g., logging) without modifying the result.
    /// </summary>
    /// <example>
    /// <code>
    /// result.Tap(val => Log($"Got {val}"));
    /// </code>
    /// </example>
    public static Result<T> Tap<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess) action(result.Value);
        return result;
    }

    /// <summary>
    /// Executes an async action (e.g., publishing event) without modifying the result.
    /// </summary>
    public static async Task<Result<T>> TapAsync<T>(this Task<Result<T>> resultTask, Func<T, Task> action)
    {
        var result = await resultTask;
        if (result.IsSuccess) await action(result.Value);
        return result;
    }

    // --- ASYNC CHAINING ---

    /// <summary>
    /// Transforms a Task&lt;Result&lt;T&gt;&gt; into Task&lt;Result&lt;U&gt;&gt; asynchronously.
    /// </summary>
    /// <example>
    /// <code>
    /// await GetUserAsync(id).MapAsync(u => u.Name);
    /// </code>
    /// </example>
    public static async Task<Result<U>> MapAsync<T, U>(this Task<Result<T>> resultTask, Func<T, Task<U>> mapper)
    {
        var result = await resultTask;
        if (result.IsFailure) return Result<U>.Failure(result.Error);
        var newValue = await mapper(result.Value);
        return Result<U>.Success(newValue);
    }

    /// <summary>
    /// Chains a Task&lt;Result&lt;T&gt;&gt; with another async Result-returning function.
    /// </summary>
    /// <example>
    /// <code>
    /// await GetUserAsync(id).BindAsync(u => UpdateUserAsync(u));
    /// </code>
    /// </example>
    public static async Task<Result<U>> BindAsync<T, U>(this Task<Result<T>> resultTask, Func<T, Task<Result<U>>> next)
    {
        var result = await resultTask;
        if (result.IsFailure) return Result<U>.Failure(result.Error);
        return await next(result.Value);
    }

    /// <summary>
    /// Matches the result of a Task&lt;Result&lt;T&gt;&gt; asynchronously.
    /// </summary>
    public static async Task<TOutput> MatchAsync<T, TOutput>(
        this Task<Result<T>> resultTask,
        Func<T, TOutput> onSuccess,
        Func<string, TOutput> onFailure)
    {
        var result = await resultTask;
        return result.Match(onSuccess, onFailure);
    }
}