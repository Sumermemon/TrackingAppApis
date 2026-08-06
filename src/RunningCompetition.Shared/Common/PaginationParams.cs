namespace RunningCompetition.Shared.Common;

/// <summary>
/// Common pagination query parameters.
/// </summary>
public class PaginationParams
{
    private const int MaxPageSize = 100;
    private int _pageSize = 20;

    /// <summary>Gets or sets the current page (1-based).</summary>
    public int Page { get; set; } = 1;

    /// <summary>Gets or sets the number of items per page (max 100).</summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }

    /// <summary>Gets or sets an optional search term.</summary>
    public string? Search { get; set; }

    /// <summary>Gets or sets the field to sort by.</summary>
    public string? SortBy { get; set; }

    /// <summary>Gets or sets the sort direction.</summary>
    public string SortDirection { get; set; } = "asc";

    /// <summary>Gets a value indicating whether sorting is descending.</summary>
    public bool IsDescending => SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
}
