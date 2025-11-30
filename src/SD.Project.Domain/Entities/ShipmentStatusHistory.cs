namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a historical record of a shipment status change.
/// Used for audit and support purposes to track the full history of shipping status changes.
/// </summary>
public class ShipmentStatusHistory
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The shipment (sub-order) this status change belongs to.
    /// </summary>
    public Guid ShipmentId { get; private set; }

    /// <summary>
    /// The parent order ID.
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// The previous status before the change.
    /// </summary>
    public ShipmentStatus PreviousStatus { get; private set; }

    /// <summary>
    /// The new status after the change.
    /// </summary>
    public ShipmentStatus NewStatus { get; private set; }

    /// <summary>
    /// When the status change occurred.
    /// </summary>
    public DateTime ChangedAt { get; private set; }

    /// <summary>
    /// The user who made the change (seller, admin, or system).
    /// Null if the change was made by the system.
    /// </summary>
    public Guid? ChangedByUserId { get; private set; }

    /// <summary>
    /// The type of actor who made the change.
    /// </summary>
    public StatusChangeActorType ActorType { get; private set; }

    /// <summary>
    /// Optional carrier name if updated during this status change.
    /// </summary>
    public string? CarrierName { get; private set; }

    /// <summary>
    /// Optional tracking number if updated during this status change.
    /// </summary>
    public string? TrackingNumber { get; private set; }

    /// <summary>
    /// Optional tracking URL if updated during this status change.
    /// </summary>
    public string? TrackingUrl { get; private set; }

    /// <summary>
    /// Optional notes or comments about the status change.
    /// </summary>
    public string? Notes { get; private set; }

    private ShipmentStatusHistory()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new shipment status history record.
    /// </summary>
    public ShipmentStatusHistory(
        Guid shipmentId,
        Guid orderId,
        ShipmentStatus previousStatus,
        ShipmentStatus newStatus,
        Guid? changedByUserId,
        StatusChangeActorType actorType,
        string? carrierName = null,
        string? trackingNumber = null,
        string? trackingUrl = null,
        string? notes = null)
    {
        if (shipmentId == Guid.Empty)
        {
            throw new ArgumentException("Shipment ID is required.", nameof(shipmentId));
        }

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order ID is required.", nameof(orderId));
        }

        Id = Guid.NewGuid();
        ShipmentId = shipmentId;
        OrderId = orderId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        ChangedAt = DateTime.UtcNow;
        ChangedByUserId = changedByUserId;
        ActorType = actorType;
        CarrierName = carrierName;
        TrackingNumber = trackingNumber;
        TrackingUrl = trackingUrl;
        Notes = notes?.Trim();
    }
}

/// <summary>
/// Type of actor who made the status change.
/// </summary>
public enum StatusChangeActorType
{
    /// <summary>Change made by the seller.</summary>
    Seller,
    /// <summary>Change made by an admin/support agent.</summary>
    Admin,
    /// <summary>Change made automatically by the system.</summary>
    System
}
