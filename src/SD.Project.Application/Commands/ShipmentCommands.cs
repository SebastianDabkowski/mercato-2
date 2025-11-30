namespace SD.Project.Application.Commands;

/// <summary>
/// Command to update a shipment's fulfilment status.
/// </summary>
public sealed record UpdateShipmentStatusCommand(
    Guid StoreId,
    Guid ShipmentId,
    string NewStatus,
    Guid UpdatedByUserId,
    string? CarrierName = null,
    string? TrackingNumber = null,
    string? TrackingUrl = null);

/// <summary>
/// Command to update tracking information for a shipped order.
/// </summary>
public sealed record UpdateTrackingInfoCommand(
    Guid StoreId,
    Guid ShipmentId,
    Guid UpdatedByUserId,
    string? CarrierName,
    string? TrackingNumber,
    string? TrackingUrl);

/// <summary>
/// Command to cancel a shipment (before it is shipped).
/// </summary>
public sealed record CancelShipmentCommand(
    Guid StoreId,
    Guid ShipmentId,
    Guid CancelledByUserId);
