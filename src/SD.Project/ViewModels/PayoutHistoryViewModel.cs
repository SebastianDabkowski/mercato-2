namespace SD.Project.ViewModels;

/// <summary>
/// View model for payout list item in history page.
/// </summary>
public sealed record PayoutHistoryListItemViewModel(
    Guid Id,
    decimal TotalAmount,
    string Currency,
    string Status,
    DateTime ScheduledDate,
    string PayoutMethod,
    int ItemCount,
    DateTime? PaidAt,
    DateTime? FailedAt,
    string? ErrorMessage);

/// <summary>
/// View model for detailed payout view with order breakdown.
/// </summary>
public sealed record PayoutHistoryDetailsViewModel(
    Guid Id,
    Guid StoreId,
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
    bool CanRetry,
    IReadOnlyList<PayoutOrderBreakdownViewModel> OrderBreakdown,
    DateTime CreatedAt,
    DateTime? ProcessedAt,
    DateTime? PaidAt,
    DateTime? FailedAt,
    DateTime? NextRetryAt);

/// <summary>
/// View model for order breakdown within a payout.
/// </summary>
public sealed record PayoutOrderBreakdownViewModel(
    Guid EscrowAllocationId,
    Guid ShipmentId,
    string? OrderNumber,
    decimal SellerAmount,
    decimal ShippingAmount,
    decimal CommissionAmount,
    decimal PayoutAmount,
    DateTime CreatedAt);
