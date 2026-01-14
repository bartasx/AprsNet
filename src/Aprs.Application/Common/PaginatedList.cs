using Microsoft.EntityFrameworkCore;

namespace Aprs.Application.Common;

/// <summary>
/// Represents a paginated list of items.
/// </summary>
/// <typeparam name="T">The type of items in the list.</typeparam>
public sealed class PaginatedList<T>
{
    public IReadOnlyList<T> Items { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages { get; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PaginatedList(IReadOnlyList<T> items, int count, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = count;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
    }

    /// <summary>
    /// Creates a paginated list from an IQueryable source.
    /// </summary>
    public static async Task<PaginatedList<T>> CreateAsync(
        IQueryable<T> source,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var count = await source.CountAsync(cancellationToken);
        var items = await source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<T>(items, count, pageNumber, pageSize);
    }

    /// <summary>
    /// Creates a paginated list from an enumerable with known total count.
    /// </summary>
    public static PaginatedList<T> Create(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
    {
        return new PaginatedList<T>(items.ToList(), totalCount, pageNumber, pageSize);
    }
}

// Extension for IQueryable to support EF Core async operations
file static class QueryableExtensions
{
    public static Task<int> CountAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken)
    {
        return EntityFrameworkQueryableExtensions.CountAsync(source, cancellationToken);
    }

    public static Task<List<T>> ToListAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken)
    {
        return EntityFrameworkQueryableExtensions.ToListAsync(source, cancellationToken);
    }
}
