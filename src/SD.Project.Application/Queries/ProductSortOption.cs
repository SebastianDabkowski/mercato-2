namespace SD.Project.Application.Queries;

/// <summary>
/// Available sorting options for product listings.
/// </summary>
public enum ProductSortOption
{
    /// <summary>
    /// Sort by relevance (default for search - matches are ranked by text relevance).
    /// For non-search contexts, falls back to Newest.
    /// </summary>
    Relevance = 0,

    /// <summary>
    /// Sort by price in ascending order (lowest first).
    /// </summary>
    PriceAscending = 1,

    /// <summary>
    /// Sort by price in descending order (highest first).
    /// </summary>
    PriceDescending = 2,

    /// <summary>
    /// Sort by creation date in descending order (newest first).
    /// Default for category browsing.
    /// </summary>
    Newest = 3
}
