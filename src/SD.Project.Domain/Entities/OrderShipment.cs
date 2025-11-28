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

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }

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
    /// </summary>
    public void Ship(string? carrierName, string? trackingNumber, string? trackingUrl)
    {
        if (Status != ShipmentStatus.Pending && Status != ShipmentStatus.Processing)
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
    /// Starts processing the shipment.
    /// </summary>
    public void StartProcessing()
    {
        if (Status != ShipmentStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot start processing shipment in status {Status}.");
        }

        Status = ShipmentStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Status of a shipment.
/// </summary>
public enum ShipmentStatus
{
    /// <summary>Awaiting seller action.</summary>
    Pending,
    /// <summary>Being prepared by seller.</summary>
    Processing,
    /// <summary>Shipped and in transit.</summary>
    Shipped,
    /// <summary>Delivered to buyer.</summary>
    Delivered,
    /// <summary>Cancelled.</summary>
    Cancelled
}
