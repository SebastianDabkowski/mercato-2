namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get a specific return request by ID for a buyer.
/// </summary>
public sealed record GetBuyerReturnRequestQuery(
    Guid BuyerId,
    Guid ReturnRequestId);

/// <summary>
/// Query to get return request status for a specific sub-order.
/// </summary>
public sealed record GetReturnRequestByShipmentQuery(
    Guid BuyerId,
    Guid ShipmentId);

/// <summary>
/// Query to get all return requests for a buyer.
/// </summary>
public sealed record GetBuyerReturnRequestsQuery(
    Guid BuyerId);

/// <summary>
/// Query to check if a sub-order is eligible for return.
/// </summary>
public sealed record CheckReturnEligibilityQuery(
    Guid BuyerId,
    Guid OrderId,
    Guid ShipmentId);

/// <summary>
/// Query to get all return requests for a store.
/// </summary>
public sealed record GetSellerReturnRequestsQuery(
    Guid StoreId,
    string? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int PageNumber = 1,
    int PageSize = 20);

/// <summary>
/// Query to get a specific return request for a seller.
/// </summary>
public sealed record GetSellerReturnRequestDetailsQuery(
    Guid StoreId,
    Guid ReturnRequestId);

/// <summary>
/// Query to get return request by shipment ID for a seller.
/// </summary>
public sealed record GetSellerReturnRequestByShipmentQuery(
    Guid StoreId,
    Guid ShipmentId);
