namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get item-level status details for a sub-order.
/// Enables viewing partial fulfilment status (Phase 2).
/// </summary>
public sealed record GetSubOrderItemStatusQuery(
    Guid StoreId,
    Guid ShipmentId);

/// <summary>
/// Query to get available status transitions for a specific item.
/// </summary>
public sealed record GetItemStatusTransitionsQuery(
    Guid StoreId,
    Guid ShipmentId,
    Guid ItemId);

/// <summary>
/// Query to calculate refund amounts for cancelled items.
/// </summary>
public sealed record CalculatePartialRefundQuery(
    Guid StoreId,
    Guid ShipmentId,
    IReadOnlyList<Guid>? ItemIds = null);

/// <summary>
/// Query to get fulfilment summary for a sub-order.
/// Shows item counts by status for quick overview.
/// </summary>
public sealed record GetSubOrderFulfilmentSummaryQuery(
    Guid StoreId,
    Guid ShipmentId);
