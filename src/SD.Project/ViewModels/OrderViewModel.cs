namespace SD.Project.ViewModels;

/// <summary>
/// View model for buyer's order details page with seller sub-order breakdown.
/// </summary>
public sealed record OrderDetailsViewModel(
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
    IReadOnlyList<SellerSubOrderViewModel> SellerSubOrders,
    DateTime? CancelledAt,
    DateTime? RefundedAt,
    decimal? RefundedAmount);

/// <summary>
/// View model for a seller sub-order section.
/// </summary>
public sealed record SellerSubOrderViewModel(
    Guid SubOrderId,
    Guid StoreId,
    string StoreName,
    string Status,
    decimal Subtotal,
    decimal ShippingCost,
    decimal Total,
    IReadOnlyList<OrderItemViewModel> Items,
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
/// View model for an order item.
/// </summary>
public sealed record OrderItemViewModel(
    Guid ProductId,
    string ProductName,
    Guid StoreId,
    string StoreName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    string? ShippingMethodName,
    decimal ShippingCost,
    string? EstimatedDelivery);

/// <summary>
/// View model for seller's sub-order list.
/// </summary>
public sealed record SellerSubOrderListViewModel(
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
/// View model for seller's sub-order details.
/// Includes buyer contact info and activity log timestamps.
/// </summary>
public sealed record SellerSubOrderDetailsViewModel(
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
    IReadOnlyList<SellerSubOrderItemViewModel> Items,
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
/// View model for an item in seller's sub-order.
/// </summary>
public sealed record SellerSubOrderItemViewModel(
    Guid ItemId,
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    string? ShippingMethodName);

/// <summary>
/// View model for buyer's order list item.
/// </summary>
public sealed record BuyerOrderListItemViewModel(
    Guid OrderId,
    string OrderNumber,
    string Status,
    int ItemCount,
    decimal TotalAmount,
    string Currency,
    DateTime CreatedAt);

/// <summary>
/// View model for admin order details with full status history.
/// </summary>
public sealed record AdminOrderDetailsViewModel(
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
    IReadOnlyList<AdminShipmentViewModel> Shipments);

/// <summary>
/// View model for a shipment in admin order details with full status history.
/// </summary>
public sealed record AdminShipmentViewModel(
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
    IReadOnlyList<AdminOrderItemViewModel> Items,
    IReadOnlyList<ShipmentStatusHistoryViewModel> StatusHistory);

/// <summary>
/// View model for an item in admin order details.
/// </summary>
public sealed record AdminOrderItemViewModel(
    Guid ItemId,
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    string? ShippingMethodName,
    string Status);

/// <summary>
/// View model for a shipping status change event in admin view.
/// </summary>
public sealed record ShipmentStatusHistoryViewModel(
    Guid Id,
    Guid ShipmentId,
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
/// View model for a shipping label.
/// </summary>
public sealed record ShippingLabelViewModel(
    Guid LabelId,
    Guid ShipmentId,
    string Format,
    string? LabelSize,
    string? TrackingNumber,
    string? CarrierName,
    DateTime GeneratedAt,
    DateTime? ExpiresAt,
    bool IsValid,
    bool IsVoided,
    int AccessCount);
