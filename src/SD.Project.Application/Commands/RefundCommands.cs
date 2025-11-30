namespace SD.Project.Application.Commands;

/// <summary>
/// Command to initiate a full refund for an order.
/// Used by support agents to process full refunds.
/// </summary>
public sealed record InitiateFullRefundCommand(
    Guid OrderId,
    Guid InitiatedById,
    string InitiatorType,
    string Reason);

/// <summary>
/// Command to initiate a partial refund for an order or shipment.
/// Used by support agents or sellers to process partial refunds.
/// </summary>
public sealed record InitiatePartialRefundCommand(
    Guid OrderId,
    Guid? ShipmentId,
    decimal Amount,
    Guid InitiatedById,
    string InitiatorType,
    string Reason);

/// <summary>
/// Command for a seller to initiate a refund within business rules.
/// Sellers can only refund their own shipments and within allowed limits.
/// </summary>
public sealed record SellerInitiateRefundCommand(
    Guid ShipmentId,
    Guid StoreId,
    Guid SellerId,
    decimal? Amount,
    string Reason);

/// <summary>
/// Command to retry a failed refund.
/// </summary>
public sealed record RetryRefundCommand(
    Guid RefundId,
    Guid InitiatedById);

/// <summary>
/// Command to process a pending refund with the payment provider.
/// Internal command used by the refund processing service.
/// </summary>
public sealed record ProcessRefundCommand(
    Guid RefundId);
