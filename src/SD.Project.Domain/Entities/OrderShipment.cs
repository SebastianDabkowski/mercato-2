namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a shipment (sub-order) from a single seller.
/// Orders with multiple sellers have multiple shipments.
/// </summary>
public class OrderShipment
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid StoreId { get; private set; }

    /// <summary>
    /// Status of this shipment.
    /// </summary>
    public ShipmentStatus Status { get; private set; }

    /// <summary>
    /// Subtotal for items from this seller.
    /// </summary>
    public decimal Subtotal { get; private set; }

    /// <summary>
    /// Shipping cost for this seller's items.
    /// </summary>
    public decimal ShippingCost { get; private set; }

    /// <summary>
    /// Carrier/courier name.
    /// </summary>
    public string? CarrierName { get; private set; }

    /// <summary>
    /// Tracking number for the shipment.
    /// </summary>
    public string? TrackingNumber { get; private set; }

    /// <summary>
    /// URL to track the shipment.
    /// </summary>
    public string? TrackingUrl { get; private set; }

    /// <summary>
    /// The shipping provider ID if created via provider integration.
    /// Null for manually tracked shipments.
    /// </summary>
    public Guid? ShippingProviderId { get; private set; }

    /// <summary>
    /// The shipment ID returned by the external provider API.
    /// Used to correlate status updates from the provider.
    /// </summary>
    public string? ProviderShipmentId { get; private set; }

    /// <summary>
    /// The label URL or data from the provider for printing shipping labels.
    /// </summary>
    public string? LabelUrl { get; private set; }

    /// <summary>
    /// Last status update received from the shipping provider.
    /// </summary>
    public string? ProviderStatus { get; private set; }

    /// <summary>
    /// When the last status update was received from the provider.
    /// </summary>
    public DateTime? ProviderStatusUpdatedAt { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public DateTime? RefundedAt { get; private set; }

    /// <summary>
    /// The refunded amount if the shipment has been refunded.
    /// </summary>
    public decimal? RefundedAmount { get; private set; }

    private OrderShipment()
    {
        // EF Core constructor
    }

    public OrderShipment(
        Guid orderId,
        Guid storeId,
        decimal subtotal,
        decimal shippingCost)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order ID is required.", nameof(orderId));
        }

        if (storeId == Guid.Empty)
        {
            throw new ArgumentException("Store ID is required.", nameof(storeId));
        }

        if (subtotal < 0)
        {
            throw new ArgumentException("Subtotal cannot be negative.", nameof(subtotal));
        }

        if (shippingCost < 0)
        {
            throw new ArgumentException("Shipping cost cannot be negative.", nameof(shippingCost));
        }

        Id = Guid.NewGuid();
        OrderId = orderId;
        StoreId = storeId;
        Status = ShipmentStatus.Pending;
        Subtotal = subtotal;
        ShippingCost = shippingCost;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the shipment as shipped with tracking information.
    /// Can only be done from Paid or Processing (preparing) status.
    /// </summary>
    public void Ship(string? carrierName, string? trackingNumber, string? trackingUrl)
    {
        if (Status != ShipmentStatus.Paid && Status != ShipmentStatus.Processing)
        {
            throw new InvalidOperationException($"Cannot ship shipment in status {Status}.");
        }

        Status = ShipmentStatus.Shipped;
        CarrierName = carrierName;
        TrackingNumber = trackingNumber;
        TrackingUrl = trackingUrl;
        ShippedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the shipment as delivered.
    /// </summary>
    public void MarkDelivered()
    {
        if (Status != ShipmentStatus.Shipped)
        {
            throw new InvalidOperationException($"Cannot mark as delivered shipment in status {Status}.");
        }

        Status = ShipmentStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates tracking information for a shipped order.
    /// Can only be done when the shipment is in Shipped status.
    /// </summary>
    public void UpdateTrackingInfo(string? carrierName, string? trackingNumber, string? trackingUrl)
    {
        if (Status != ShipmentStatus.Shipped)
        {
            throw new InvalidOperationException($"Cannot update tracking info for shipment in status {Status}. Shipment must be shipped first.");
        }

        CarrierName = carrierName;
        TrackingNumber = trackingNumber;
        TrackingUrl = trackingUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the shipment as shipped via a shipping provider.
    /// Associates the shipment with provider details and tracking information.
    /// </summary>
    public void ShipViaProvider(
        Guid shippingProviderId,
        string providerShipmentId,
        string? carrierName,
        string? trackingNumber,
        string? trackingUrl,
        string? labelUrl)
    {
        if (Status != ShipmentStatus.Paid && Status != ShipmentStatus.Processing)
        {
            throw new InvalidOperationException($"Cannot ship shipment in status {Status}.");
        }

        if (shippingProviderId == Guid.Empty)
        {
            throw new ArgumentException("Shipping provider ID is required.", nameof(shippingProviderId));
        }

        if (string.IsNullOrWhiteSpace(providerShipmentId))
        {
            throw new ArgumentException("Provider shipment ID is required.", nameof(providerShipmentId));
        }

        Status = ShipmentStatus.Shipped;
        ShippingProviderId = shippingProviderId;
        ProviderShipmentId = providerShipmentId;
        CarrierName = carrierName;
        TrackingNumber = trackingNumber;
        TrackingUrl = trackingUrl;
        LabelUrl = labelUrl;
        ShippedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the provider status for this shipment.
    /// Called when receiving status updates from the shipping provider.
    /// </summary>
    public void UpdateProviderStatus(string providerStatus)
    {
        if (string.IsNullOrWhiteSpace(providerStatus))
        {
            throw new ArgumentException("Provider status is required.", nameof(providerStatus));
        }

        ProviderStatus = providerStatus;
        ProviderStatusUpdatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the shipment as in transit based on provider status update.
    /// Only applicable for shipped shipments.
    /// </summary>
    public void MarkInTransit(string? providerStatus = null)
    {
        if (Status != ShipmentStatus.Shipped)
        {
            throw new InvalidOperationException($"Cannot mark as in transit shipment in status {Status}.");
        }

        if (!string.IsNullOrWhiteSpace(providerStatus))
        {
            ProviderStatus = providerStatus;
            ProviderStatusUpdatedAt = DateTime.UtcNow;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Starts processing the shipment (seller preparing shipment).
    /// Can only be done from Paid status.
    /// </summary>
    public void StartProcessing()
    {
        if (Status != ShipmentStatus.Paid)
        {
            throw new InvalidOperationException($"Cannot start processing shipment in status {Status}.");
        }

        Status = ShipmentStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Confirms payment and sets the shipment to Paid status.
    /// Can only be done from Pending status.
    /// </summary>
    public void ConfirmPayment()
    {
        if (Status != ShipmentStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot confirm payment for shipment in status {Status}.");
        }

        Status = ShipmentStatus.Paid;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancels the shipment. Can only be done before shipping.
    /// </summary>
    public void Cancel()
    {
        if (Status == ShipmentStatus.Shipped || Status == ShipmentStatus.Delivered || 
            Status == ShipmentStatus.Refunded || Status == ShipmentStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot cancel shipment in status {Status}.");
        }

        Status = ShipmentStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Refunds the shipment. Sets status to 'refunded' and records the refunded amount.
    /// Can be called after payment has been confirmed (paid, preparing, shipped, delivered).
    /// </summary>
    /// <param name="refundedAmount">The amount being refunded. If null, defaults to Subtotal + ShippingCost.</param>
    public void Refund(decimal? refundedAmount = null)
    {
        // Can only refund shipments that have been paid
        if (Status == ShipmentStatus.Pending || Status == ShipmentStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot refund shipment in status {Status}.");
        }

        if (Status == ShipmentStatus.Refunded)
        {
            throw new InvalidOperationException("Shipment has already been refunded.");
        }

        var totalAmount = Subtotal + ShippingCost;
        var amount = refundedAmount ?? totalAmount;
        if (amount <= 0)
        {
            throw new ArgumentException("Refunded amount must be greater than zero.", nameof(refundedAmount));
        }

        if (amount > totalAmount)
        {
            throw new ArgumentException("Refunded amount cannot exceed shipment total.", nameof(refundedAmount));
        }

        Status = ShipmentStatus.Refunded;
        RefundedAmount = amount;
        RefundedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the shipment can transition to the specified status.
    /// </summary>
    public bool CanTransitionTo(ShipmentStatus targetStatus)
    {
        return targetStatus switch
        {
            ShipmentStatus.Pending => false, // Cannot go back to pending
            ShipmentStatus.Paid => Status == ShipmentStatus.Pending,
            ShipmentStatus.Processing => Status == ShipmentStatus.Paid,
            ShipmentStatus.Shipped => Status == ShipmentStatus.Paid || Status == ShipmentStatus.Processing,
            ShipmentStatus.Delivered => Status == ShipmentStatus.Shipped,
            ShipmentStatus.Cancelled => CanBeCancelled(),
            ShipmentStatus.Refunded => CanBeRefunded(),
            _ => false
        };
    }

    /// <summary>
    /// Checks if the shipment is in a state that allows cancellation.
    /// Shipments cannot be cancelled once shipped, delivered, refunded, or already cancelled.
    /// </summary>
    private bool CanBeCancelled()
    {
        return Status != ShipmentStatus.Shipped && 
               Status != ShipmentStatus.Delivered && 
               Status != ShipmentStatus.Refunded && 
               Status != ShipmentStatus.Cancelled;
    }

    /// <summary>
    /// Checks if the shipment is in a state that allows refund.
    /// Shipments can be refunded after payment is confirmed.
    /// </summary>
    private bool CanBeRefunded()
    {
        return Status == ShipmentStatus.Paid || 
               Status == ShipmentStatus.Processing || 
               Status == ShipmentStatus.Shipped || 
               Status == ShipmentStatus.Delivered;
    }
}

/// <summary>
/// Status of a shipment (sub-order).
/// Statuses: new (Pending), paid (Paid), preparing (Processing), shipped, delivered, cancelled, refunded.
/// State transitions are constrained (e.g., cannot move from delivered back to preparing).
/// </summary>
public enum ShipmentStatus
{
    /// <summary>Awaiting payment confirmation (new).</summary>
    Pending,
    /// <summary>Payment confirmed, awaiting seller action (paid).</summary>
    Paid,
    /// <summary>Being prepared by seller (preparing).</summary>
    Processing,
    /// <summary>Shipped and in transit.</summary>
    Shipped,
    /// <summary>Delivered to buyer.</summary>
    Delivered,
    /// <summary>Cancelled.</summary>
    Cancelled,
    /// <summary>Refunded.</summary>
    Refunded
}
