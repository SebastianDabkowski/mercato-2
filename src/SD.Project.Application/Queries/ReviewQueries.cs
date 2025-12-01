namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get reviews for a product.
/// </summary>
public record GetProductReviewsQuery(Guid ProductId);

/// <summary>
/// Query to get reviews for a store.
/// </summary>
public record GetStoreReviewsQuery(Guid StoreId);

/// <summary>
/// Query to get rating summary for a product.
/// </summary>
public record GetProductRatingQuery(Guid ProductId);

/// <summary>
/// Query to get rating summary for a store.
/// </summary>
public record GetStoreRatingQuery(Guid StoreId);

/// <summary>
/// Query to check if a buyer can submit a review for a specific order item.
/// </summary>
public record CheckReviewEligibilityQuery(
    Guid BuyerId,
    Guid OrderId,
    Guid ShipmentId,
    Guid ProductId);
