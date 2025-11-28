namespace SD.Project.Domain.Repositories;

/// <summary>
/// Sort options for product queries.
/// </summary>
public enum ProductSortOrder
{
    /// <summary>
    /// Sort by creation date descending (newest first). Default sort order.
    /// </summary>
    Newest = 0,

    /// <summary>
    /// Sort by price in ascending order (lowest first).
    /// </summary>
    PriceAscending = 1,

    /// <summary>
    /// Sort by price in descending order (highest first).
    /// </summary>
    PriceDescending = 2
}
