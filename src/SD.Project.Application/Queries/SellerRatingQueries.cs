namespace SD.Project.Application.Queries;

/// <summary>
/// Query to check if a buyer can submit a seller rating for a specific order.
/// </summary>
public record CheckSellerRatingEligibilityQuery(
    Guid BuyerId,
    Guid OrderId);

/// <summary>
/// Query to get the seller rating stats for a store.
/// </summary>
public record GetSellerRatingStatsQuery(Guid StoreId);
