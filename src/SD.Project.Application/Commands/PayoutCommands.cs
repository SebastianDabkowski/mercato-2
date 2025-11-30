namespace SD.Project.Application.Commands;

/// <summary>
/// Command to schedule a payout for a seller's eligible escrow allocations.
/// </summary>
public sealed record SchedulePayoutCommand(
    Guid StoreId,
    Guid SellerId);

/// <summary>
/// Command to process a scheduled payout.
/// </summary>
public sealed record ProcessPayoutCommand(
    Guid PayoutId);

/// <summary>
/// Command to process all payouts scheduled for today.
/// </summary>
public sealed record ProcessScheduledPayoutsCommand;

/// <summary>
/// Command to retry failed payouts that are due for retry.
/// </summary>
public sealed record RetryFailedPayoutsCommand;

/// <summary>
/// Command to manually mark a payout as paid (admin operation).
/// </summary>
public sealed record MarkPayoutPaidCommand(
    Guid PayoutId,
    string? PayoutReference);

/// <summary>
/// Command to manually mark a payout as failed (admin operation).
/// </summary>
public sealed record MarkPayoutFailedCommand(
    Guid PayoutId,
    string? ErrorReference,
    string? ErrorMessage);
