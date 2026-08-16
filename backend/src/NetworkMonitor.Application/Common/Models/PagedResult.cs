namespace NetworkMonitor.Application.Common.Models;

/// <summary>
/// Standard shape for every paginated list response — matches the
/// { items, page, pageSize, totalCount } contract documented in docs/api.md.
/// </summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalCount) =>
        new()
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
}

/// <summary>Common paging + search parameters accepted by list queries.</summary>
public class PagedRequest
{
    private const int MaxPageSize = 100;
    private int _pageSize = 25;

    public int Page { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value is > 0 and <= MaxPageSize ? value : _pageSize;
    }

    public string? Search { get; set; }
}
