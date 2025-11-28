namespace SD.Project.Application.DTOs;

/// <summary>
/// Represents a paginated result set with metadata about the pagination state.
/// </summary>
/// <typeparam name="T">The type of items in the result set.</typeparam>
public sealed record PagedResultDto<T>
{
    /// <summary>
    /// The items on the current page.
    /// </summary>
    public IReadOnlyCollection<T> Items { get; init; } = Array.Empty<T>();

    /// <summary>
    /// The current page number (1-based).
    /// </summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// The number of items per page.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// The total number of items across all pages.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// The total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Whether there is a previous page available.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Whether there is a next page available.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Creates a new paginated result.
    /// </summary>
    public static PagedResultDto<T> Create(IReadOnlyCollection<T> items, int pageNumber, int pageSize, int totalCount)
    {
        return new PagedResultDto<T>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
