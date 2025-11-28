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
/// Query to filter and search products.
/// Combines text search with filter criteria.
/// </summary>
/// <param name="SearchTerm">Optional search term to match against product name and description.</param>
/// <param name="Filters">Optional filter criteria.</param>
public sealed record FilterProductsQuery(
    string? SearchTerm = null,
    ProductFilterCriteria? Filters = null);
