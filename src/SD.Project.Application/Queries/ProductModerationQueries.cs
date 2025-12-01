using SD.Project.Domain.Entities;

namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get products awaiting moderation with pagination and filtering.
/// </summary>
public record GetProductsForModerationQuery(
    ProductModerationStatus? Status = null,
    string? Category = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 20);

/// <summary>
/// Query to get product moderation statistics.
/// </summary>
public record GetProductModerationStatsQuery;

/// <summary>
/// Query to get product moderation audit history for a specific product.
/// </summary>
public record GetProductModerationHistoryQuery(Guid ProductId);

/// <summary>
/// Query to get product details for moderation review.
/// </summary>
public record GetProductForModerationDetailsQuery(Guid ProductId);
