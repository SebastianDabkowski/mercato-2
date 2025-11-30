namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get refund by ID.
/// </summary>
public sealed record GetRefundByIdQuery(
    Guid RefundId);

/// <summary>
/// Query to get all refunds for an order.
/// </summary>
public sealed record GetOrderRefundsQuery(
    Guid OrderId);

/// <summary>
/// Query to get refund summary for an order.
/// </summary>
public sealed record GetOrderRefundSummaryQuery(
    Guid OrderId);

/// <summary>
/// Query to get refunds for a store with optional status filter.
/// </summary>
public sealed record GetStoreRefundsQuery(
    Guid StoreId,
    string? Status,
    int PageNumber = 1,
    int PageSize = 20);

/// <summary>
/// Query to validate if a seller can initiate a refund.
/// </summary>
public sealed record ValidateSellerRefundQuery(
    Guid ShipmentId,
    Guid StoreId,
    decimal? RequestedAmount);

/// <summary>
/// Query to get pending refunds that need processing.
/// </summary>
public sealed record GetPendingRefundsQuery(
    int Take = 100);

/// <summary>
/// Query to get failed refunds that can be retried.
/// </summary>
public sealed record GetRetryableRefundsQuery(
    int Take = 100);
