namespace SD.Project.ViewModels;

/// <summary>
/// View model for buyer's return request display.
/// </summary>
public sealed record BuyerReturnRequestViewModel(
    Guid ReturnRequestId,
    Guid OrderId,
    Guid ShipmentId,
    string OrderNumber,
    string StoreName,
    string Status,
    string Reason,
    string? Comments,
    string? SellerResponse,
    DateTime CreatedAt,
    DateTime? ApprovedAt,
    DateTime? RejectedAt,
    DateTime? CompletedAt);

/// <summary>
/// View model for return eligibility check.
/// </summary>
public sealed record ReturnEligibilityViewModel(
    bool IsEligible,
    string? IneligibilityReason,
    DateTime? ReturnWindowEndsAt,
    bool HasExistingReturnRequest,
    string? ExistingReturnStatus);

/// <summary>
/// View model for seller's return request summary in list.
/// </summary>
public sealed record SellerReturnRequestSummaryViewModel(
    Guid ReturnRequestId,
    Guid OrderId,
    string OrderNumber,
    string Status,
    string BuyerName,
    string Reason,
    decimal SubOrderTotal,
    string Currency,
    DateTime CreatedAt);

/// <summary>
/// View model for seller's return request details.
/// </summary>
public sealed record SellerReturnRequestDetailsViewModel(
    Guid ReturnRequestId,
    Guid OrderId,
    Guid ShipmentId,
    string OrderNumber,
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
    IReadOnlyList<SellerSubOrderItemViewModel> Items);
