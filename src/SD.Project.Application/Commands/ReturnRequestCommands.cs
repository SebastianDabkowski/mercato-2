namespace SD.Project.Application.Commands;

/// <summary>
/// Command to initiate a return request for a sub-order.
/// </summary>
public sealed record InitiateReturnRequestCommand(
    Guid BuyerId,
    Guid OrderId,
    Guid ShipmentId,
    string Reason,
    string? Comments = null);

/// <summary>
/// Command for a seller to approve a return request.
/// </summary>
public sealed record ApproveReturnRequestCommand(
    Guid StoreId,
    Guid ReturnRequestId,
    string? SellerResponse = null);

/// <summary>
/// Command for a seller to reject a return request.
/// </summary>
public sealed record RejectReturnRequestCommand(
    Guid StoreId,
    Guid ReturnRequestId,
    string RejectionReason);

/// <summary>
/// Command to mark a return as completed.
/// </summary>
public sealed record CompleteReturnRequestCommand(
    Guid StoreId,
    Guid ReturnRequestId);
