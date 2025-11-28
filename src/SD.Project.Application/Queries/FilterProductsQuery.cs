namespace SD.Project.Application.Queries;

/// <summary>
/// Criteria for filtering products. All properties are optional.
/// </summary>
/// <param name="Category">Filter by category name.</param>
/// <param name="MinPrice">Minimum price (inclusive).</param>
/// <param name="MaxPrice">Maximum price (inclusive).</param>
/// <param name="Condition">Filter by product status/condition (e.g., "Active", "Draft").</param>
/// <param name="StoreId">Filter by seller/store ID.</param>
public sealed record ProductFilterCriteria(
    string? Category = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? Condition = null,
    Guid? StoreId = null);

/// <summary>
/// Query to filter and search products.
/// Combines text search with filter criteria.
/// </summary>
/// <param name="SearchTerm">Optional search term to match against product name and description.</param>
/// <param name="Filters">Optional filter criteria.</param>
public sealed record FilterProductsQuery(
    string? SearchTerm = null,
    ProductFilterCriteria? Filters = null);
