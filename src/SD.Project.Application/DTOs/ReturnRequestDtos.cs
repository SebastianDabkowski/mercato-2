namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO for return request information displayed to buyer.
/// </summary>
public sealed record BuyerReturnRequestDto(
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
/// DTO for return request information displayed to seller.
/// </summary>
public sealed record SellerReturnRequestDto(
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
    IReadOnlyList<SellerSubOrderItemDto> Items);

/// <summary>
/// DTO for return request list summary for seller.
/// </summary>
public sealed record SellerReturnRequestSummaryDto(
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
/// Result DTO for initiating a return request.
/// </summary>
public sealed record InitiateReturnResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    Guid? ReturnRequestId = null);

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
