namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO for escrow payment information.
/// </summary>
public sealed record EscrowPaymentDto(
    Guid Id,
    Guid OrderId,
    Guid BuyerId,
    decimal TotalAmount,
    string Currency,
    string Status,
    decimal ReleasedAmount,
    decimal RefundedAmount,
    IReadOnlyList<EscrowAllocationDto> Allocations,
    DateTime CreatedAt,
    DateTime? ReleasedAt,
    DateTime? RefundedAt);

/// <summary>
/// DTO for escrow allocation per seller.
/// </summary>
public sealed record EscrowAllocationDto(
    Guid Id,
    Guid StoreId,
    Guid ShipmentId,
    string Currency,
    decimal SellerAmount,
    decimal ShippingAmount,
    decimal TotalAmount,
    decimal CommissionAmount,
    decimal CommissionRate,
    decimal SellerPayout,
    decimal RefundedAmount,
    decimal RefundedCommissionAmount,
    string Status,
    bool IsEligibleForPayout,
    DateTime CreatedAt,
    DateTime? ReleasedAt,
    DateTime? RefundedAt,
    DateTime? PayoutEligibleAt);

/// <summary>
/// Summary DTO for seller's pending escrow balance.
/// </summary>
public sealed record SellerEscrowBalanceDto(
    Guid StoreId,
    decimal TotalHeldAmount,
    decimal TotalEligibleForPayout,
    decimal TotalCommissions,
    int HeldAllocationsCount,
    int EligibleAllocationsCount,
    string Currency);

/// <summary>
/// DTO for an escrow ledger entry (audit log).
/// </summary>
public sealed record EscrowLedgerEntryDto(
    Guid Id,
    Guid EscrowPaymentId,
    Guid? AllocationId,
    string Action,
    decimal Amount,
    string Currency,
    string? Reference,
    string? Notes,
    DateTime Timestamp);

/// <summary>
/// Result DTO for escrow creation.
/// </summary>
public sealed record CreateEscrowResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    Guid? EscrowPaymentId)
{
    public static CreateEscrowResultDto Succeeded(Guid escrowPaymentId) =>
        new(true, null, escrowPaymentId);

    public static CreateEscrowResultDto Failed(string errorMessage) =>
        new(false, errorMessage, null);
}

/// <summary>
/// Result DTO for escrow release operations.
/// </summary>
public sealed record ReleaseEscrowResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    decimal? ReleasedAmount,
    string? PayoutReference)
{
    public static ReleaseEscrowResultDto Succeeded(decimal releasedAmount, string? payoutReference) =>
        new(true, null, releasedAmount, payoutReference);

    public static ReleaseEscrowResultDto Failed(string errorMessage) =>
        new(false, errorMessage, null, null);
}

/// <summary>
/// Result DTO for escrow refund operations.
/// </summary>
public sealed record RefundEscrowResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    decimal? RefundedAmount,
    string? RefundReference)
{
    public static RefundEscrowResultDto Succeeded(decimal refundedAmount, string? refundReference) =>
        new(true, null, refundedAmount, refundReference);

    public static RefundEscrowResultDto Failed(string errorMessage) =>
        new(false, errorMessage, null, null);
}

/// <summary>
/// Result DTO for partial escrow refund operations with commission details.
/// </summary>
public sealed record PartialRefundEscrowResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    decimal? RefundedAmount,
    decimal? RefundedCommissionAmount,
    decimal? RemainingAmount,
    decimal? RemainingCommission,
    string? RefundReference)
{
    public static PartialRefundEscrowResultDto Succeeded(
        decimal refundedAmount,
        decimal refundedCommissionAmount,
        decimal remainingAmount,
        decimal remainingCommission,
        string? refundReference) =>
        new(true, null, refundedAmount, refundedCommissionAmount, remainingAmount, remainingCommission, refundReference);

    public static PartialRefundEscrowResultDto Failed(string errorMessage) =>
        new(false, errorMessage, null, null, null, null, null);
}
