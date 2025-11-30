namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO representing a seller sub-order (shipment) for seller view.
/// Includes buyer contact information and delivery details for fulfilment.
/// </summary>
public sealed record SellerSubOrderDto(
    Guid SubOrderId,
    Guid OrderId,
    string OrderNumber,
    string Status,
    string PaymentStatus,
    decimal Subtotal,
    decimal ShippingCost,
    decimal Total,
    string Currency,
    string BuyerName,
    string? BuyerEmail,
    string? BuyerPhone,
    string DeliveryAddress,
    string? DeliveryInstructions,
    string? ShippingMethodName,
    IReadOnlyList<SellerSubOrderItemDto> Items,
    DateTime CreatedAt,
    DateTime? PaidAt,
    DateTime? ShippedAt,
    DateTime? DeliveredAt,
    DateTime? CancelledAt,
    DateTime? RefundedAt,
    string? CarrierName,
    string? TrackingNumber,
    string? TrackingUrl);

/// <summary>
/// DTO representing an item in a seller sub-order.
/// Includes item-level fulfilment status for Phase 2 partial fulfilment.
/// </summary>
public sealed record SellerSubOrderItemDto(
    Guid ItemId,
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    string? ShippingMethodName,
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
/// DTO for seller's sub-order list summary view.
/// </summary>
public sealed record SellerSubOrderSummaryDto(
    Guid SubOrderId,
    Guid OrderId,
    string OrderNumber,
    string Status,
    int ItemCount,
    decimal Total,
    string Currency,
    string BuyerName,
    DateTime CreatedAt);

/// <summary>
/// DTO for buyer's order details view with seller sub-order breakdown.
/// </summary>
public sealed record BuyerOrderDetailsDto(
    Guid OrderId,
    string OrderNumber,
    string Status,
    string PaymentStatus,
    string PaymentStatusMessage,
    string RecipientName,
    string DeliveryAddressSummary,
    string PaymentMethodName,
    decimal ItemSubtotal,
    decimal TotalShipping,
    decimal TotalAmount,
    string Currency,
    DateTime CreatedAt,
    string? EstimatedDeliveryRange,
    IReadOnlyList<SellerSubOrderSectionDto> SellerSubOrders,
    DateTime? CancelledAt,
    DateTime? RefundedAt,
    decimal? RefundedAmount);

/// <summary>
/// DTO for a seller sub-order section in buyer's order details view.
/// </summary>
public sealed record SellerSubOrderSectionDto(
    Guid SubOrderId,
    Guid StoreId,
    string StoreName,
    string Status,
    decimal Subtotal,
    decimal ShippingCost,
    decimal Total,
    IReadOnlyList<OrderItemDto> Items,
    string? CarrierName,
    string? TrackingNumber,
    string? TrackingUrl,
    DateTime? ShippedAt,
    DateTime? DeliveredAt,
    string? EstimatedDelivery,
    DateTime? CancelledAt,
    DateTime? RefundedAt,
    decimal? RefundedAmount);

/// <summary>
/// DTO representing a single shipping status change event.
/// </summary>
public sealed record ShipmentStatusHistoryDto(
    Guid Id,
    Guid ShipmentId,
    Guid OrderId,
    string PreviousStatus,
    string NewStatus,
    DateTime ChangedAt,
    Guid? ChangedByUserId,
    string? ChangedByUserName,
    string ActorType,
    string? CarrierName,
    string? TrackingNumber,
    string? TrackingUrl,
    string? Notes);

/// <summary>
/// DTO for admin order details view with full status history.
/// </summary>
public sealed record AdminOrderDetailsDto(
    Guid OrderId,
    string OrderNumber,
    string Status,
    string PaymentStatus,
    Guid BuyerId,
    string BuyerName,
    string? BuyerEmail,
    string RecipientName,
    string DeliveryAddressSummary,
    string PaymentMethodName,
    string? PaymentTransactionId,
    decimal ItemSubtotal,
    decimal TotalShipping,
    decimal TotalAmount,
    string Currency,
    DateTime CreatedAt,
    DateTime? PaidAt,
    DateTime? CancelledAt,
    DateTime? RefundedAt,
    decimal? RefundedAmount,
    IReadOnlyList<AdminShipmentDto> Shipments);

/// <summary>
/// DTO for a shipment in admin order details view with full status history.
/// </summary>
public sealed record AdminShipmentDto(
    Guid ShipmentId,
    Guid StoreId,
    string StoreName,
    string Status,
    decimal Subtotal,
    decimal ShippingCost,
    decimal Total,
    string? CarrierName,
    string? TrackingNumber,
    string? TrackingUrl,
    DateTime CreatedAt,
    DateTime? ShippedAt,
    DateTime? DeliveredAt,
    DateTime? CancelledAt,
    DateTime? RefundedAt,
    decimal? RefundedAmount,
    IReadOnlyList<AdminOrderItemDto> Items,
    IReadOnlyList<ShipmentStatusHistoryDto> StatusHistory);

/// <summary>
/// DTO for an item in admin order details view.
/// </summary>
public sealed record AdminOrderItemDto(
    Guid ItemId,
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    string? ShippingMethodName,
    string Status);
