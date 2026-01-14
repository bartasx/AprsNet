namespace Aprs.Domain.Common;

/// <summary>
/// Represents the result of an operation that can either succeed or fail.
/// </summary>
/// <typeparam name="T">The type of the value on success.</typeparam>
public sealed class Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    private Result(T value)
    {
        IsSuccess = true;
        _value = value;
        _error = null;
    }

    private Result(Error error)
    {
        IsSuccess = false;
        _value = default;
        _error = error;
    }

    /// <summary>
    /// Gets a value indicating whether the result is successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the result is a failure.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the value if the result is successful.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing value on a failed result.</exception>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access value of a failed result.");

    /// <summary>
    /// Gets the error if the result is a failure.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing error on a successful result.</exception>
    public Error Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access error of a successful result.");

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    public static Result<T> Failure(Error error) => new(error);

    /// <summary>
    /// Creates a failed result with the specified error code and message.
    /// </summary>
    public static Result<T> Failure(string code, string message) => new(new Error(code, message));

    /// <summary>
    /// Implicit conversion from value to successful result.
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>
    /// Implicit conversion from error to failed result.
    /// </summary>
    public static implicit operator Result<T>(Error error) => Failure(error);

    /// <summary>
    /// Maps the result value to a new type if successful.
    /// </summary>
    public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        return IsSuccess
            ? Result<TNew>.Success(mapper(Value))
            : Result<TNew>.Failure(Error);
    }

    /// <summary>
    /// Executes the specified action if the result is successful.
    /// </summary>
    public Result<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess)
        {
            action(Value);
        }
        return this;
    }

    /// <summary>
    /// Executes the specified action if the result is a failure.
    /// </summary>
    public Result<T> OnFailure(Action<Error> action)
    {
        if (IsFailure)
        {
            action(Error);
        }
        return this;
    }

    /// <summary>
    /// Matches the result to one of two functions.
    /// </summary>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value) : onFailure(Error);
    }
}

/// <summary>
/// Represents the result of an operation without a value.
/// </summary>
public sealed class Result
{
    private readonly Error? _error;

    private Result()
    {
        IsSuccess = true;
        _error = null;
    }

    private Result(Error error)
    {
        IsSuccess = false;
        _error = error;
    }

    /// <summary>
    /// Gets a value indicating whether the result is successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the result is a failure.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error if the result is a failure.
    /// </summary>
    public Error Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access error of a successful result.");

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new();

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    public static Result Failure(Error error) => new(error);

    /// <summary>
    /// Creates a failed result with the specified error code and message.
    /// </summary>
    public static Result Failure(string code, string message) => new(new Error(code, message));

    /// <summary>
    /// Implicit conversion from error to failed result.
    /// </summary>
    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>
/// Represents an error with a code and message.
/// </summary>
/// <param name="Code">The error code.</param>
/// <param name="Message">The error message.</param>
public sealed record Error(string Code, string Message)
{
    /// <summary>
    /// Represents no error (null object pattern).
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>
    /// Creates a validation error.
    /// </summary>
    public static Error Validation(string message) => new("Validation", message);

    /// <summary>
    /// Creates a not found error.
    /// </summary>
    public static Error NotFound(string message) => new("NotFound", message);

    /// <summary>
    /// Creates a conflict error.
    /// </summary>
    public static Error Conflict(string message) => new("Conflict", message);

    /// <summary>
    /// Creates an internal error.
    /// </summary>
    public static Error Internal(string message) => new("Internal", message);
}
