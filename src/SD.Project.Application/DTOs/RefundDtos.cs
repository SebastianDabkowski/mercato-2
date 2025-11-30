namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO for refund information.
/// </summary>
public sealed record RefundDto(
    Guid Id,
    Guid OrderId,
    Guid? ShipmentId,
    Guid BuyerId,
    Guid? StoreId,
    string Type,
    string Status,
    decimal Amount,
    string Currency,
    decimal CommissionRefundAmount,
    string Reason,
    string? RefundTransactionId,
    Guid InitiatedById,
    string InitiatorType,
    string? ErrorMessage,
    string? ErrorCode,
    int RetryCount,
    bool CanRetry,
    DateTime CreatedAt,
    DateTime? CompletedAt);

/// <summary>
/// Result DTO for initiating a refund.
/// </summary>
public sealed record InitiateRefundResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    Guid? RefundId,
    string? RefundTransactionId,
    string? Status)
{
    public static InitiateRefundResultDto Succeeded(Guid refundId, string? refundTransactionId, string status) =>
        new(true, null, refundId, refundTransactionId, status);

    public static InitiateRefundResultDto Failed(string errorMessage) =>
        new(false, errorMessage, null, null, null);
}

/// <summary>
/// Result DTO for processing a refund.
/// </summary>
public sealed record ProcessRefundResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    string? RefundTransactionId,
    string Status,
    decimal? RefundedAmount,
    decimal? RefundedCommission)
{
    public static ProcessRefundResultDto Succeeded(
        string refundTransactionId, 
        string status, 
        decimal refundedAmount, 
        decimal refundedCommission) =>
        new(true, null, refundTransactionId, status, refundedAmount, refundedCommission);

    public static ProcessRefundResultDto Pending(string status) =>
        new(true, null, null, status, null, null);

    public static ProcessRefundResultDto Failed(string errorMessage, string status) =>
        new(false, errorMessage, null, status, null, null);
}

/// <summary>
/// Summary DTO for refund history on an order.
/// </summary>
public sealed record OrderRefundSummaryDto(
    Guid OrderId,
    decimal TotalOrderAmount,
    decimal TotalRefundedAmount,
    decimal RemainingAmount,
    int RefundCount,
    IReadOnlyList<RefundDto> Refunds,
    string Currency);

/// <summary>
/// Business rules validation result for seller-initiated refunds.
/// </summary>
public sealed record SellerRefundValidationDto(
    bool IsAllowed,
    string? ValidationMessage,
    decimal MaxRefundableAmount,
    bool RequiresApproval);
