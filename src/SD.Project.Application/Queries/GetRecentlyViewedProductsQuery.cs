namespace SD.Project.Application.Queries;

/// <summary>
/// Query to retrieve recently viewed products by their IDs.
/// Returns only active products in the order specified.
/// </summary>
public sealed record GetRecentlyViewedProductsQuery(IReadOnlyList<Guid> ProductIds);
