namespace RunningCompetition.Shared.Common;

/// <summary>
/// Represents a paginated collection of items.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class PagedList<T>
{
    /// <summary>Gets the items on the current page.</summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>Gets the current page number (1-based).</summary>
    public int Page { get; }

    /// <summary>Gets the number of items per page.</summary>
    public int PageSize { get; }

    /// <summary>Gets the total number of items across all pages.</summary>
    public int TotalCount { get; }

    /// <summary>Gets the total number of pages.</summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>Gets a value indicating whether there is a previous page.</summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>Gets a value indicating whether there is a next page.</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>Initializes a new instance of <see cref="PagedList{T}"/>.</summary>
    public PagedList(IEnumerable<T> items, int totalCount, int page, int pageSize)
    {
        Items = items.ToList().AsReadOnly();
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    /// <summary>Creates a <see cref="PagedList{T}"/> from an <see cref="IQueryable{T}"/> source.</summary>
    public static async Task<PagedList<T>> CreateAsync(
        IQueryable<T> source,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var count = source.Count();
        var items = source.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedList<T>(items, count, page, pageSize);
    }
}
