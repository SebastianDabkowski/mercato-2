namespace SD.Project.Application.Commands;

/// <summary>
/// Command to update an individual item's fulfilment status.
/// Enables partial fulfilment within a sub-order (Phase 2).
/// </summary>
public sealed record UpdateItemStatusCommand(
    Guid StoreId,
    Guid ShipmentId,
    Guid ItemId,
    string NewStatus,
    Guid UpdatedByUserId,
    string? CarrierName = null,
    string? TrackingNumber = null,
    string? TrackingUrl = null);

/// <summary>
/// Command to update multiple items' status at once.
/// Enables batch partial fulfilment operations.
/// </summary>
public sealed record BatchUpdateItemStatusCommand(
    Guid StoreId,
    Guid ShipmentId,
    IReadOnlyList<Guid> ItemIds,
    string NewStatus,
    Guid UpdatedByUserId,
    string? CarrierName = null,
    string? TrackingNumber = null,
    string? TrackingUrl = null);

/// <summary>
/// Command to cancel specific items within a sub-order.
/// </summary>
public sealed record CancelItemsCommand(
    Guid StoreId,
    Guid ShipmentId,
    IReadOnlyList<Guid> ItemIds,
    Guid CancelledByUserId);

/// <summary>
/// Command to refund specific cancelled items within a sub-order.
/// </summary>
public sealed record RefundItemsCommand(
    Guid StoreId,
    Guid ShipmentId,
    IReadOnlyList<Guid> ItemIds,
    Guid RefundedByUserId);

/// <summary>
/// Command to update tracking info for specific shipped items.
/// </summary>
public sealed record UpdateItemsTrackingCommand(
    Guid StoreId,
    Guid ShipmentId,
    IReadOnlyList<Guid> ItemIds,
    Guid UpdatedByUserId,
    string? CarrierName,
    string? TrackingNumber,
    string? TrackingUrl);
