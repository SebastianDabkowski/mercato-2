namespace SD.Project.Application.Commands;

/// <summary>
/// Represents an item to be included in a return/complaint request.
/// </summary>
public sealed record ReturnRequestItemInput(
    Guid OrderItemId,
    string ProductName,
    int Quantity);

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
/// Command to submit a return or complaint request with item selection.
/// This is the new command supporting type selection and item-level granularity.
/// </summary>
public sealed record SubmitReturnOrComplaintCommand(
    Guid BuyerId,
    Guid OrderId,
    Guid ShipmentId,
    string RequestType,
    string Reason,
    string? Description,
    IReadOnlyList<ReturnRequestItemInput> Items);

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
