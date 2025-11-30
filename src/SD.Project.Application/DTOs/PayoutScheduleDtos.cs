using SD.Project.Domain.Entities;

namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO for seller payout information.
/// </summary>
public sealed record SellerPayoutDto(
    Guid Id,
    Guid StoreId,
    Guid SellerId,
    decimal TotalAmount,
    string Currency,
    string Status,
    DateTime ScheduledDate,
    string PayoutMethod,
    string? PayoutReference,
    string? ErrorReference,
    string? ErrorMessage,
    int RetryCount,
    int MaxRetries,
    IReadOnlyList<SellerPayoutItemDto> Items,
    DateTime CreatedAt,
    DateTime? ProcessedAt,
    DateTime? PaidAt,
    DateTime? FailedAt,
    DateTime? NextRetryAt);

/// <summary>
/// DTO for seller payout item.
/// </summary>
public sealed record SellerPayoutItemDto(
    Guid Id,
    Guid EscrowAllocationId,
    decimal Amount,
    DateTime CreatedAt);

/// <summary>
/// Summary DTO for seller's payout status.
/// </summary>
public sealed record SellerPayoutSummaryDto(
    Guid StoreId,
    decimal PendingAmount,
    decimal ScheduledAmount,
    decimal ProcessingAmount,
    int ScheduledPayoutsCount,
    int FailedPayoutsCount,
    DateTime? NextScheduledDate,
    string Currency);

/// <summary>
/// Result DTO for payout schedule operations.
/// </summary>
public sealed record SchedulePayoutResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    Guid? PayoutId,
    decimal? TotalAmount,
    DateTime? ScheduledDate,
    int ItemCount)
{
    public static SchedulePayoutResultDto Succeeded(
        Guid payoutId,
        decimal totalAmount,
        DateTime scheduledDate,
        int itemCount) =>
        new(true, null, payoutId, totalAmount, scheduledDate, itemCount);

    public static SchedulePayoutResultDto Failed(string errorMessage) =>
        new(false, errorMessage, null, null, null, 0);

    public static SchedulePayoutResultDto BelowThreshold(decimal currentBalance, decimal threshold) =>
        new(false, $"Balance {currentBalance:F2} is below minimum payout threshold {threshold:F2}. Balance will roll over to the next payout period.", null, null, null, 0);
}

/// <summary>
/// Result DTO for processing payout operations.
/// </summary>
public sealed record ProcessPayoutResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    Guid? PayoutId,
    decimal? PaidAmount,
    string? PayoutReference)
{
    public static ProcessPayoutResultDto Succeeded(Guid payoutId, decimal paidAmount, string? payoutReference) =>
        new(true, null, payoutId, paidAmount, payoutReference);

    public static ProcessPayoutResultDto Failed(Guid payoutId, string errorMessage) =>
        new(false, errorMessage, payoutId, null, null);
}

/// <summary>
/// Configuration for payout schedule settings.
/// </summary>
public sealed record PayoutScheduleConfigDto(
    PayoutScheduleFrequency Frequency,
    DayOfWeek PayoutDay,
    decimal MinimumPayoutThreshold,
    string Currency);
