namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the fulfilment status of an individual order item.
/// Enables partial fulfilment within a sub-order (Phase 2).
/// </summary>
public enum OrderItemStatus
{
    /// <summary>Item is newly created and awaiting processing (new).</summary>
    New,
    /// <summary>Item is being prepared for shipment (preparing).</summary>
    Preparing,
    /// <summary>Item has been shipped.</summary>
    Shipped,
    /// <summary>Item has been delivered.</summary>
    Delivered,
    /// <summary>Item was cancelled by seller before shipment.</summary>
    Cancelled,
    /// <summary>Item has been refunded.</summary>
    Refunded
}

/// <summary>
/// Represents an item in an order.
/// </summary>
public class OrderItem
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid StoreId { get; private set; }

    /// <summary>
    /// Product name at time of order (denormalized for historical record).
    /// </summary>
    public string ProductName { get; private set; } = default!;

    /// <summary>
    /// Unit price at time of order.
    /// </summary>
    public decimal UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    /// <summary>
    /// Line total (unit price × quantity).
    /// </summary>
    public decimal LineTotal { get; private set; }

    /// <summary>
    /// Selected shipping method for this item/seller.
    /// </summary>
    public Guid? ShippingMethodId { get; private set; }

    /// <summary>
    /// Shipping method name for display.
    /// </summary>
    public string? ShippingMethodName { get; private set; }

    /// <summary>
    /// Shipping cost for this item.
    /// </summary>
    public decimal ShippingCost { get; private set; }

    /// <summary>
    /// Estimated delivery time in business days (minimum) at time of order.
    /// </summary>
    public int? EstimatedDeliveryDaysMin { get; private set; }

    /// <summary>
    /// Estimated delivery time in business days (maximum) at time of order.
    /// </summary>
    public int? EstimatedDeliveryDaysMax { get; private set; }

    /// <summary>
    /// Current fulfilment status of this item (Phase 2: partial fulfilment).
    /// </summary>
    public OrderItemStatus Status { get; private set; }

    /// <summary>
    /// When the item status was last updated.
    /// </summary>
    public DateTime? StatusUpdatedAt { get; private set; }

    /// <summary>
    /// When the item was cancelled (if cancelled).
    /// </summary>
    public DateTime? CancelledAt { get; private set; }

    /// <summary>
    /// When the item was refunded (if refunded).
    /// </summary>
    public DateTime? RefundedAt { get; private set; }

    /// <summary>
    /// The refunded amount if the item has been refunded.
    /// </summary>
    public decimal? RefundedAmount { get; private set; }

    /// <summary>
    /// Carrier/courier name for this item's shipment.
    /// </summary>
    public string? CarrierName { get; private set; }

    /// <summary>
    /// Tracking number for this item's shipment.
    /// </summary>
    public string? TrackingNumber { get; private set; }

    /// <summary>
    /// URL to track this item's shipment.
    /// </summary>
    public string? TrackingUrl { get; private set; }

    /// <summary>
    /// When the item was shipped (if shipped).
    /// </summary>
    public DateTime? ShippedAt { get; private set; }

    /// <summary>
    /// When the item was delivered (if delivered).
    /// </summary>
    public DateTime? DeliveredAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private OrderItem()
    {
        // EF Core constructor
    }

    public OrderItem(
        Guid orderId,
        Guid productId,
        Guid storeId,
        string productName,
        decimal unitPrice,
        int quantity,
        Guid? shippingMethodId = null,
        string? shippingMethodName = null,
        decimal shippingCost = 0m,
        int? estimatedDeliveryDaysMin = null,
        int? estimatedDeliveryDaysMax = null)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order ID is required.", nameof(orderId));
        }

        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product ID is required.", nameof(productId));
        }

        if (storeId == Guid.Empty)
        {
            throw new ArgumentException("Store ID is required.", nameof(storeId));
        }

        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new ArgumentException("Product name is required.", nameof(productName));
        }

        if (unitPrice < 0)
        {
            throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));
        }

        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }

        if (shippingCost < 0)
        {
            throw new ArgumentException("Shipping cost cannot be negative.", nameof(shippingCost));
        }

        Id = Guid.NewGuid();
        OrderId = orderId;
        ProductId = productId;
        StoreId = storeId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
        LineTotal = unitPrice * quantity;
        ShippingMethodId = shippingMethodId;
        ShippingMethodName = shippingMethodName;
        ShippingCost = shippingCost;
        EstimatedDeliveryDaysMin = estimatedDeliveryDaysMin;
        EstimatedDeliveryDaysMax = estimatedDeliveryDaysMax;
        Status = OrderItemStatus.New;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the item as being prepared for shipment.
    /// Can only be done from New status.
    /// </summary>
    public void MarkPreparing()
    {
        if (Status != OrderItemStatus.New)
        {
            throw new InvalidOperationException($"Cannot mark item as preparing in status {Status}.");
        }

        Status = OrderItemStatus.Preparing;
        StatusUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the item as shipped with optional tracking information.
    /// Can only be done from New or Preparing status.
    /// </summary>
    public void Ship(string? carrierName = null, string? trackingNumber = null, string? trackingUrl = null)
    {
        if (Status != OrderItemStatus.New && Status != OrderItemStatus.Preparing)
        {
            throw new InvalidOperationException($"Cannot ship item in status {Status}.");
        }

        Status = OrderItemStatus.Shipped;
        CarrierName = carrierName;
        TrackingNumber = trackingNumber;
        TrackingUrl = trackingUrl;
        ShippedAt = DateTime.UtcNow;
        StatusUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates tracking information for a shipped item.
    /// </summary>
    public void UpdateTrackingInfo(string? carrierName, string? trackingNumber, string? trackingUrl)
    {
        if (Status != OrderItemStatus.Shipped)
        {
            throw new InvalidOperationException($"Cannot update tracking info for item in status {Status}. Item must be shipped first.");
        }

        CarrierName = carrierName;
        TrackingNumber = trackingNumber;
        TrackingUrl = trackingUrl;
        StatusUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the item as delivered.
    /// Can only be done from Shipped status.
    /// </summary>
    public void MarkDelivered()
    {
        if (Status != OrderItemStatus.Shipped)
        {
            throw new InvalidOperationException($"Cannot mark item as delivered in status {Status}.");
        }

        Status = OrderItemStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
        StatusUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancels the item. Can only be done before shipment.
    /// </summary>
    public void Cancel()
    {
        if (Status == OrderItemStatus.Shipped || Status == OrderItemStatus.Delivered ||
            Status == OrderItemStatus.Cancelled || Status == OrderItemStatus.Refunded)
        {
            throw new InvalidOperationException($"Cannot cancel item in status {Status}.");
        }

        Status = OrderItemStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        StatusUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Refunds the item. Sets status to 'refunded' and records the refunded amount.
    /// Can be called after the item is cancelled, shipped or delivered.
    /// </summary>
    /// <param name="refundedAmount">The amount being refunded. If null, defaults to LineTotal + ShippingCost.</param>
    public void Refund(decimal? refundedAmount = null)
    {
        // Can only refund items that are cancelled, shipped or delivered
        if (Status == OrderItemStatus.New || Status == OrderItemStatus.Preparing)
        {
            throw new InvalidOperationException($"Cannot refund item in status {Status}. Item must be cancelled, shipped, or delivered first.");
        }

        if (Status == OrderItemStatus.Refunded)
        {
            throw new InvalidOperationException("Item has already been refunded.");
        }

        var totalAmount = LineTotal + ShippingCost;
        var amount = refundedAmount ?? totalAmount;
        if (amount <= 0)
        {
            throw new ArgumentException("Refunded amount must be greater than zero.", nameof(refundedAmount));
        }

        if (amount > totalAmount)
        {
            throw new ArgumentException("Refunded amount cannot exceed item total.", nameof(refundedAmount));
        }

        Status = OrderItemStatus.Refunded;
        RefundedAmount = amount;
        RefundedAt = DateTime.UtcNow;
        StatusUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the item can transition to the specified status.
    /// </summary>
    public bool CanTransitionTo(OrderItemStatus targetStatus)
    {
        return targetStatus switch
        {
            OrderItemStatus.New => false, // Cannot go back to new
            OrderItemStatus.Preparing => Status == OrderItemStatus.New,
            OrderItemStatus.Shipped => Status == OrderItemStatus.New || Status == OrderItemStatus.Preparing,
            OrderItemStatus.Delivered => Status == OrderItemStatus.Shipped,
            OrderItemStatus.Cancelled => CanBeCancelled(),
            OrderItemStatus.Refunded => CanBeRefunded(),
            _ => false
        };
    }

    /// <summary>
    /// Checks if the item is in a state that allows cancellation.
    /// Items cannot be cancelled once shipped, delivered, refunded, or already cancelled.
    /// </summary>
    private bool CanBeCancelled()
    {
        return Status != OrderItemStatus.Shipped &&
               Status != OrderItemStatus.Delivered &&
               Status != OrderItemStatus.Refunded &&
               Status != OrderItemStatus.Cancelled;
    }

    /// <summary>
    /// Checks if the item is in a state that allows refund.
    /// Items can be refunded after being cancelled, shipped, or delivered.
    /// </summary>
    private bool CanBeRefunded()
    {
        return Status == OrderItemStatus.Cancelled ||
               Status == OrderItemStatus.Shipped ||
               Status == OrderItemStatus.Delivered;
    }

    /// <summary>
    /// Gets the total amount eligible for refund (LineTotal + ShippingCost).
    /// </summary>
    public decimal GetRefundableAmount()
    {
        return LineTotal + ShippingCost;
    }

    /// <summary>
    /// Gets a formatted estimated delivery time string.
    /// Returns null if no delivery time information is available.
    /// </summary>
    public string? GetEstimatedDeliveryDisplay()
    {
        if (!EstimatedDeliveryDaysMin.HasValue || !EstimatedDeliveryDaysMax.HasValue)
        {
            return null;
        }

        if (EstimatedDeliveryDaysMin == EstimatedDeliveryDaysMax)
        {
            return EstimatedDeliveryDaysMin == 1
                ? "1 business day"
                : $"{EstimatedDeliveryDaysMin} business days";
        }

        return $"{EstimatedDeliveryDaysMin}-{EstimatedDeliveryDaysMax} business days";
    }
}
