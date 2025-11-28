namespace SD.Project.Application.Queries;

/// <summary>
/// Criteria for filtering active products. All properties are optional.
/// Only Active products are returned for public views.
/// </summary>
/// <param name="Category">Filter by category name.</param>
/// <param name="MinPrice">Minimum price (inclusive).</param>
/// <param name="MaxPrice">Maximum price (inclusive).</param>
/// <param name="StoreId">Filter by seller/store ID.</param>
public sealed record ProductFilterCriteria(
    string? Category = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    Guid? StoreId = null);

/// <summary>
/// Query to filter and search products with pagination support.
/// Combines text search with filter criteria.
/// </summary>
/// <param name="SearchTerm">Optional search term to match against product name and description.</param>
/// <param name="Filters">Optional filter criteria.</param>
/// <param name="SortBy">Sort order for results. Defaults to Relevance for search, Newest for category browsing.</param>
/// <param name="PageNumber">Page number (1-based). Defaults to 1.</param>
/// <param name="PageSize">Number of items per page. Defaults to 12.</param>
public sealed record FilterProductsQuery(
    string? SearchTerm = null,
    ProductFilterCriteria? Filters = null,
    ProductSortOption? SortBy = null,
    int PageNumber = 1,
    int PageSize = 12);
