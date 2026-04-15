namespace Project.Application.Common;


public class PaginationParams
{
    private const int MaxPageSize = 100;
    private int _pageSize = 20;
    public int Page { get; set; } = 1;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize
                         : value < 1 ? 1
                         : value;
    }
    public int Offset => (Page - 1) * PageSize;
}



public sealed class PagedResult<T>
{

    public IEnumerable<T> Items { get; init; } = [];

    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;


    public static PagedResult<T> Create(IEnumerable<T> items, int totalCount, PaginationParams pagination)
        => new()
        {
            Items = items,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
}