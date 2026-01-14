namespace Aprs.Domain.Common;

/// <summary>
/// Abstraction for DateTime operations to enable testability.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>
    /// Gets the current UTC date and time.
    /// </summary>
    DateTime UtcNow { get; }

    /// <summary>
    /// Gets the current local date and time.
    /// </summary>
    DateTime Now { get; }

    /// <summary>
    /// Gets today's date.
    /// </summary>
    DateOnly Today { get; }
}

/// <summary>
/// Default implementation of <see cref="IDateTimeProvider"/> using system clock.
/// </summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    /// <inheritdoc/>
    public DateTime UtcNow => DateTime.UtcNow;

    /// <inheritdoc/>
    public DateTime Now => DateTime.Now;

    /// <inheritdoc/>
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
}
