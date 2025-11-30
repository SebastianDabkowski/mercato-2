namespace SD.Project.Application.Commands;

/// <summary>
/// Command to release escrow funds to a seller for a specific shipment.
/// Called when payout conditions are met (e.g., delivery confirmed).
/// </summary>
public sealed record ReleaseEscrowCommand(
    Guid ShipmentId,
    Guid StoreId,
    string? PayoutReference = null);

/// <summary>
/// Command to refund escrow allocation back to buyer for a specific shipment.
/// Called when a shipment is cancelled.
/// </summary>
public sealed record RefundShipmentEscrowCommand(
    Guid ShipmentId,
    string? RefundReference = null);

/// <summary>
/// Command to refund full escrow back to buyer.
/// Called when an order is cancelled.
/// </summary>
public sealed record RefundOrderEscrowCommand(
    Guid OrderId,
    string? RefundReference = null);

/// <summary>
/// Command to mark an escrow allocation as eligible for payout.
/// Called when conditions for payout are met (e.g., delivery confirmed, hold period passed).
/// </summary>
public sealed record MarkEscrowEligibleCommand(
    Guid ShipmentId);
