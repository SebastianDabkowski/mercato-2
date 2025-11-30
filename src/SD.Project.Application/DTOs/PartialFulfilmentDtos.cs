namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO for displaying order item status in buyer and seller views.
/// Includes item-level fulfilment status for Phase 2 partial fulfilment.
/// </summary>
public sealed record OrderItemStatusDto(
    Guid ItemId,
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    decimal ShippingCost,
    string Status,
    string? CarrierName,
    string? TrackingNumber,
    string? TrackingUrl,
    DateTime? ShippedAt,
    DateTime? DeliveredAt,
    DateTime? CancelledAt,
    DateTime? RefundedAt,
    decimal? RefundedAmount);

/// <summary>
/// Result DTO for item-level status update operations.
/// </summary>
public sealed record UpdateItemStatusResultDto(
    bool IsSuccess,
    string? ErrorMessage = null,
    string? PreviousStatus = null,
    string? NewStatus = null);

/// <summary>
/// DTO representing available status transitions for an order item.
/// </summary>
public sealed record ItemStatusTransitionsDto(
    string CurrentStatus,
    IReadOnlyList<string> AvailableTransitions,
    bool CanUpdateTracking,
    bool CanCancel,
    bool CanRefund);

/// <summary>
/// Result DTO for partial item refund calculation.
/// Shows the refund breakdown per cancelled item.
/// </summary>
public sealed record PartialRefundCalculationDto(
    decimal TotalRefundAmount,
    string Currency,
    IReadOnlyList<ItemRefundBreakdownDto> ItemBreakdowns);

/// <summary>
/// DTO for individual item refund breakdown.
/// </summary>
public sealed record ItemRefundBreakdownDto(
    Guid ItemId,
    string ProductName,
    decimal LineTotal,
    decimal ShippingCost,
    decimal RefundAmount);

/// <summary>
/// Summary DTO for sub-order with item-level status breakdown.
/// Used in seller view to show partial fulfilment progress.
/// </summary>
public sealed record SubOrderFulfilmentSummaryDto(
    Guid SubOrderId,
    string OverallStatus,
    int TotalItems,
    int NewItems,
    int PreparingItems,
    int ShippedItems,
    int DeliveredItems,
    int CancelledItems,
    int RefundedItems,
    bool IsPartiallyFulfilled,
    bool IsFullyShipped,
    bool IsFullyDelivered);
