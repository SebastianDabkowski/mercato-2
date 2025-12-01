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

/// <summary>
/// Command for a seller to resolve a case with a specific resolution type.
/// This can optionally initiate a refund if the resolution requires one.
/// </summary>
public sealed record ResolveCaseCommand(
    Guid StoreId,
    Guid SellerId,
    Guid ReturnRequestId,
    string ResolutionType,
    string? ResolutionNotes,
    decimal? PartialRefundAmount = null,
    bool InitiateRefund = false);

/// <summary>
/// Command to link an existing refund to a case.
/// Used when a refund was processed outside the normal case resolution flow.
/// </summary>
public sealed record LinkRefundToCaseCommand(
    Guid StoreId,
    Guid ReturnRequestId,
    Guid RefundId);

/// <summary>
/// Command to escalate a case to admin review.
/// Can be initiated by buyer (disagreeing with seller) or admin (manual flag or SLA breach).
/// </summary>
public sealed record EscalateCaseCommand(
    Guid ReturnRequestId,
    Guid EscalatedByUserId,
    string EscalationReason,
    string? Notes = null);

/// <summary>
/// Command to record an admin decision on an escalated case.
/// </summary>
public sealed record RecordAdminDecisionCommand(
    Guid ReturnRequestId,
    Guid AdminUserId,
    string DecisionType,
    string DecisionNotes,
    string? ResolutionType = null,
    decimal? PartialRefundAmount = null,
    bool InitiateRefund = false);
