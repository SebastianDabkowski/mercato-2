namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO for an item included in a return/complaint request.
/// </summary>
public sealed record ReturnRequestItemDto(
    Guid ItemId,
    Guid OrderItemId,
    string ProductName,
    int Quantity);

/// <summary>
/// DTO for return request information displayed to buyer.
/// </summary>
public sealed record BuyerReturnRequestDto(
    Guid ReturnRequestId,
    Guid OrderId,
    Guid ShipmentId,
    string CaseNumber,
    string OrderNumber,
    string StoreName,
    string Type,
    string Status,
    string Reason,
    string? Comments,
    string? SellerResponse,
    DateTime CreatedAt,
    DateTime? ApprovedAt,
    DateTime? RejectedAt,
    DateTime? CompletedAt,
    IReadOnlyList<ReturnRequestItemDto> Items);

/// <summary>
/// DTO for return request information displayed to seller.
/// </summary>
public sealed record SellerReturnRequestDto(
    Guid ReturnRequestId,
    Guid OrderId,
    Guid ShipmentId,
    string CaseNumber,
    string OrderNumber,
    string Type,
    string Status,
    string BuyerName,
    string? BuyerEmail,
    string Reason,
    string? Comments,
    string? SellerResponse,
    decimal SubOrderTotal,
    string Currency,
    DateTime CreatedAt,
    DateTime? ApprovedAt,
    DateTime? RejectedAt,
    DateTime? CompletedAt,
    IReadOnlyList<SellerSubOrderItemDto> Items,
    IReadOnlyList<ReturnRequestItemDto> RequestItems,
    string? ResolutionType = null,
    string? ResolutionNotes = null,
    decimal? PartialRefundAmount = null,
    DateTime? ResolvedAt = null,
    Guid? LinkedRefundId = null,
    bool CanChangeResolution = false,
    LinkedRefundDto? LinkedRefund = null);

/// <summary>
/// DTO for return request list summary for seller.
/// </summary>
public sealed record SellerReturnRequestSummaryDto(
    Guid ReturnRequestId,
    Guid OrderId,
    string CaseNumber,
    string OrderNumber,
    string Type,
    string Status,
    string BuyerName,
    string Reason,
    decimal SubOrderTotal,
    string Currency,
    DateTime CreatedAt);

/// <summary>
/// Result DTO for initiating a return request.
/// </summary>
public sealed record InitiateReturnResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    Guid? ReturnRequestId = null);

/// <summary>
/// Result DTO for submitting a return or complaint request.
/// </summary>
public sealed record SubmitReturnOrComplaintResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    Guid? ReturnRequestId = null,
    string? CaseNumber = null);

/// <summary>
/// Result DTO for updating return request status (approve/reject/complete).
/// </summary>
public sealed record UpdateReturnRequestResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    string? PreviousStatus = null,
    string? NewStatus = null);

/// <summary>
/// DTO for return eligibility check result.
/// </summary>
public sealed record ReturnEligibilityDto(
    bool IsEligible,
    string? IneligibilityReason,
    DateTime? ReturnWindowEndsAt,
    bool HasExistingReturnRequest,
    string? ExistingReturnStatus);

/// <summary>
/// DTO for item eligibility in return/complaint requests.
/// </summary>
public sealed record ItemEligibilityDto(
    Guid OrderItemId,
    string ProductName,
    int Quantity,
    bool IsEligible,
    string? IneligibilityReason,
    bool HasOpenCase,
    string? OpenCaseNumber);

/// <summary>
/// DTO for linked refund information in a return/complaint case.
/// </summary>
public sealed record LinkedRefundDto(
    Guid RefundId,
    string Status,
    decimal Amount,
    string Currency,
    string? RefundTransactionId,
    DateTime CreatedAt,
    DateTime? CompletedAt);

/// <summary>
/// DTO for detailed buyer case (return/complaint) with all information.
/// </summary>
public sealed record BuyerCaseDetailsDto(
    Guid ReturnRequestId,
    Guid OrderId,
    Guid ShipmentId,
    string CaseNumber,
    string OrderNumber,
    string StoreName,
    string Type,
    string Status,
    string Reason,
    string? Comments,
    string? SellerResponse,
    DateTime CreatedAt,
    DateTime? ApprovedAt,
    DateTime? RejectedAt,
    DateTime? CompletedAt,
    IReadOnlyList<ReturnRequestItemDto> Items,
    IReadOnlyList<LinkedRefundDto>? LinkedRefunds,
    string? ResolutionType = null,
    string? ResolutionNotes = null,
    decimal? PartialRefundAmount = null,
    DateTime? ResolvedAt = null);

/// <summary>
/// Result DTO for resolving a case.
/// </summary>
public sealed record ResolveCaseResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    string? ResolutionType = null,
    Guid? RefundId = null,
    string? RefundStatus = null);

/// <summary>
/// Result DTO for linking a refund to a case.
/// </summary>
public sealed record LinkRefundResultDto(
    bool IsSuccess,
    string? ErrorMessage);

/// <summary>
/// DTO for admin view of a return request summary in list.
/// </summary>
public sealed record AdminReturnRequestSummaryDto(
    Guid ReturnRequestId,
    Guid OrderId,
    Guid StoreId,
    string CaseNumber,
    string OrderNumber,
    string StoreName,
    string Type,
    string Status,
    string SellerName,
    string BuyerAlias,
    string Reason,
    decimal SubOrderTotal,
    string Currency,
    DateTime CreatedAt,
    int AgeInDays,
    bool IsEscalated,
    DateTime? EscalatedAt);

/// <summary>
/// DTO for admin view of return request details including all case data.
/// </summary>
public sealed record AdminReturnRequestDetailsDto(
    Guid ReturnRequestId,
    Guid OrderId,
    Guid ShipmentId,
    Guid StoreId,
    string CaseNumber,
    string OrderNumber,
    string StoreName,
    string Type,
    string Status,
    string SellerName,
    string? SellerEmail,
    string BuyerName,
    string? BuyerEmail,
    string Reason,
    string? Comments,
    string? SellerResponse,
    decimal SubOrderTotal,
    string Currency,
    DateTime CreatedAt,
    DateTime? ApprovedAt,
    DateTime? RejectedAt,
    DateTime? CompletedAt,
    IReadOnlyList<SellerSubOrderItemDto> Items,
    IReadOnlyList<ReturnRequestItemDto> RequestItems,
    string? ResolutionType,
    string? ResolutionNotes,
    decimal? PartialRefundAmount,
    DateTime? ResolvedAt,
    Guid? LinkedRefundId,
    LinkedRefundDto? LinkedRefund,
    // Escalation info
    bool IsEscalated,
    DateTime? EscalatedAt,
    Guid? EscalatedByUserId,
    string? EscalationReason,
    string? EscalationNotes,
    // Admin decision info
    bool HasAdminDecision,
    Guid? AdminDecisionByUserId,
    string? AdminDecision,
    string? AdminDecisionNotes,
    DateTime? AdminDecisionAt,
    // Permissions
    bool CanEscalate,
    bool CanRecordDecision);

/// <summary>
/// Result DTO for escalating a case.
/// </summary>
public sealed record EscalateCaseResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    string? NewStatus = null);

/// <summary>
/// Result DTO for recording an admin decision.
/// </summary>
public sealed record AdminDecisionResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    string? Decision = null,
    Guid? RefundId = null,
    string? RefundStatus = null);
