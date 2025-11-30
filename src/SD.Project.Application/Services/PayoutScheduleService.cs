using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for managing seller payout schedules.
/// Handles scheduling, processing, and retry of seller payouts.
/// </summary>
public sealed class PayoutScheduleService
{
    private readonly ISellerPayoutRepository _payoutRepository;
    private readonly IEscrowRepository _escrowRepository;
    private readonly IPayoutSettingsRepository _payoutSettingsRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly INotificationService _notificationService;

    /// <summary>
    /// Default minimum payout threshold in the seller's currency.
    /// Balances below this amount will roll over to the next payout period.
    /// </summary>
    public const decimal DefaultMinimumPayoutThreshold = 10.00m;

    /// <summary>
    /// Default payout schedule frequency.
    /// </summary>
    public const PayoutScheduleFrequency DefaultFrequency = PayoutScheduleFrequency.Weekly;

    /// <summary>
    /// Default day of the week for payouts.
    /// </summary>
    public const DayOfWeek DefaultPayoutDay = DayOfWeek.Friday;

    /// <summary>
    /// Default currency when none is available from allocations or payouts.
    /// </summary>
    public const string DefaultCurrency = "EUR";

    public PayoutScheduleService(
        ISellerPayoutRepository payoutRepository,
        IEscrowRepository escrowRepository,
        IPayoutSettingsRepository payoutSettingsRepository,
        IStoreRepository storeRepository,
        INotificationService notificationService)
    {
        _payoutRepository = payoutRepository;
        _escrowRepository = escrowRepository;
        _payoutSettingsRepository = payoutSettingsRepository;
        _storeRepository = storeRepository;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Schedules a payout for a seller by aggregating their eligible escrow allocations.
    /// </summary>
    public async Task<SchedulePayoutResultDto> HandleAsync(
        SchedulePayoutCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Get the store to verify it exists
        var store = await _storeRepository.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null)
        {
            return SchedulePayoutResultDto.Failed("Store not found.");
        }

        // Get payout settings to verify configuration
        var payoutSettings = await _payoutSettingsRepository.GetBySellerIdAsync(command.SellerId, cancellationToken);
        if (payoutSettings is null || !payoutSettings.IsConfigured)
        {
            return SchedulePayoutResultDto.Failed("Payout settings are not configured. Please configure payout method first.");
        }

        if (!payoutSettings.IsVerified)
        {
            return SchedulePayoutResultDto.Failed("Payout settings are not verified. Please wait for verification.");
        }

        // Get eligible escrow allocations
        var eligibleAllocations = await _escrowRepository.GetEligibleForPayoutByStoreIdAsync(
            command.StoreId, cancellationToken);

        if (eligibleAllocations.Count == 0)
        {
            return SchedulePayoutResultDto.Failed("No eligible allocations found for payout.");
        }

        // Filter out allocations already in a payout
        var allocationsToInclude = new List<EscrowAllocation>();
        foreach (var allocation in eligibleAllocations)
        {
            var isInPayout = await _payoutRepository.IsAllocationInPayoutAsync(allocation.Id, cancellationToken);
            if (!isInPayout)
            {
                allocationsToInclude.Add(allocation);
            }
        }

        if (allocationsToInclude.Count == 0)
        {
            return SchedulePayoutResultDto.Failed("All eligible allocations are already scheduled for payout.");
        }

        // Calculate total eligible amount
        var totalEligible = allocationsToInclude.Sum(a => a.GetRemainingSellerPayout());
        var currency = allocationsToInclude.First().Currency;

        // Check minimum payout threshold
        if (totalEligible < DefaultMinimumPayoutThreshold)
        {
            return SchedulePayoutResultDto.BelowThreshold(totalEligible, DefaultMinimumPayoutThreshold);
        }

        // Check for existing scheduled payout
        var existingPayout = await _payoutRepository.GetCurrentScheduledPayoutAsync(command.StoreId, cancellationToken);
        if (existingPayout is not null)
        {
            // Add allocations to existing payout
            foreach (var allocation in allocationsToInclude)
            {
                existingPayout.AddItem(allocation);
            }

            await _payoutRepository.UpdateAsync(existingPayout, cancellationToken);
            await _payoutRepository.SaveChangesAsync(cancellationToken);

            return SchedulePayoutResultDto.Succeeded(
                existingPayout.Id,
                existingPayout.TotalAmount,
                existingPayout.ScheduledDate,
                existingPayout.Items.Count);
        }

        // Calculate next scheduled date
        var scheduledDate = CalculateNextPayoutDate(DefaultFrequency, DefaultPayoutDay);

        // Create new payout
        var payout = new SellerPayout(
            command.StoreId,
            command.SellerId,
            currency,
            scheduledDate,
            payoutSettings.DefaultPayoutMethod);

        foreach (var allocation in allocationsToInclude)
        {
            payout.AddItem(allocation);
        }

        await _payoutRepository.AddAsync(payout, cancellationToken);
        await _payoutRepository.SaveChangesAsync(cancellationToken);

        // Notify seller about scheduled payout
        await _notificationService.SendPayoutScheduledNotificationAsync(
            command.SellerId,
            payout.Id,
            payout.TotalAmount,
            currency,
            scheduledDate,
            cancellationToken);

        return SchedulePayoutResultDto.Succeeded(
            payout.Id,
            payout.TotalAmount,
            payout.ScheduledDate,
            payout.Items.Count);
    }

    /// <summary>
    /// Processes a specific payout.
    /// </summary>
    public async Task<ProcessPayoutResultDto> HandleAsync(
        ProcessPayoutCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var payout = await _payoutRepository.GetByIdAsync(command.PayoutId, cancellationToken);
        if (payout is null)
        {
            return ProcessPayoutResultDto.Failed(command.PayoutId, "Payout not found.");
        }

        if (payout.Status == SellerPayoutStatus.Paid)
        {
            return ProcessPayoutResultDto.Succeeded(payout.Id, payout.TotalAmount, payout.PayoutReference);
        }

        if (payout.Status != SellerPayoutStatus.Scheduled && !payout.CanRetry())
        {
            return ProcessPayoutResultDto.Failed(command.PayoutId, $"Cannot process payout in status {payout.Status}.");
        }

        try
        {
            payout.StartProcessing();
            await _payoutRepository.UpdateAsync(payout, cancellationToken);
            await _payoutRepository.SaveChangesAsync(cancellationToken);

            // In a real implementation, this would call the payment provider
            // For now, we'll simulate success with a generated reference
            var payoutReference = GeneratePayoutReference(payout.Id);

            // Release the escrow allocations
            foreach (var item in payout.Items)
            {
                var allocation = await _escrowRepository.GetAllocationByIdAsync(
                    item.EscrowAllocationId, cancellationToken);
                
                if (allocation is not null)
                {
                    allocation.Release(payoutReference);
                    // The allocation update will be handled by the escrow repository
                    var escrow = await _escrowRepository.GetByIdAsync(allocation.EscrowPaymentId, cancellationToken);
                    if (escrow is not null)
                    {
                        await _escrowRepository.UpdateAsync(escrow, cancellationToken);
                    }
                }
            }

            payout.MarkPaid(payoutReference);
            await _payoutRepository.UpdateAsync(payout, cancellationToken);
            await _payoutRepository.SaveChangesAsync(cancellationToken);

            // Notify seller about successful payout
            await _notificationService.SendPayoutCompletedNotificationAsync(
                payout.SellerId,
                payout.Id,
                payout.TotalAmount,
                payout.Currency,
                payoutReference,
                cancellationToken);

            return ProcessPayoutResultDto.Succeeded(payout.Id, payout.TotalAmount, payoutReference);
        }
        catch (Exception ex)
        {
            payout.MarkFailed("PROCESSING_ERROR", ex.Message);
            await _payoutRepository.UpdateAsync(payout, cancellationToken);
            await _payoutRepository.SaveChangesAsync(cancellationToken);

            // Notify seller about failed payout
            await _notificationService.SendPayoutFailedNotificationAsync(
                payout.SellerId,
                payout.Id,
                payout.TotalAmount,
                payout.Currency,
                ex.Message,
                payout.CanRetry(),
                cancellationToken);

            return ProcessPayoutResultDto.Failed(payout.Id, ex.Message);
        }
    }

    /// <summary>
    /// Processes all payouts scheduled for today or earlier.
    /// </summary>
    public async Task<IReadOnlyList<ProcessPayoutResultDto>> HandleAsync(
        ProcessScheduledPayoutsCommand command,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ProcessPayoutResultDto>();

        // Process payouts scheduled for today or earlier
        var scheduledPayouts = await _payoutRepository.GetScheduledForProcessingAsync(
            DateTime.UtcNow, cancellationToken);

        foreach (var payout in scheduledPayouts)
        {
            var result = await HandleAsync(new ProcessPayoutCommand(payout.Id), cancellationToken);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// Retries failed payouts that are due for retry.
    /// </summary>
    public async Task<IReadOnlyList<ProcessPayoutResultDto>> HandleAsync(
        RetryFailedPayoutsCommand command,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ProcessPayoutResultDto>();

        var failedPayouts = await _payoutRepository.GetDueForRetryAsync(
            DateTime.UtcNow, cancellationToken);

        foreach (var payout in failedPayouts)
        {
            var result = await HandleAsync(new ProcessPayoutCommand(payout.Id), cancellationToken);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// Gets a payout by ID.
    /// </summary>
    public async Task<SellerPayoutDto?> HandleAsync(
        GetPayoutByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var payout = await _payoutRepository.GetByIdAsync(query.PayoutId, cancellationToken);
        return payout is null ? null : MapToDto(payout);
    }

    /// <summary>
    /// Gets payouts for a store with pagination.
    /// </summary>
    public async Task<PagedResultDto<SellerPayoutDto>> HandleAsync(
        GetPayoutsByStoreIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (payouts, totalCount) = await _payoutRepository.GetByStoreIdAsync(
            query.StoreId, query.Skip, query.Take, cancellationToken);

        var dtos = payouts.Select(MapToDto).ToList();

        var pageSize = query.Take > 0 ? query.Take : 20;
        var pageNumber = (query.Skip / pageSize) + 1;

        return PagedResultDto<SellerPayoutDto>.Create(dtos, pageNumber, pageSize, totalCount);
    }

    /// <summary>
    /// Gets payout summary for a seller.
    /// </summary>
    public async Task<SellerPayoutSummaryDto> HandleAsync(
        GetPayoutSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var payouts = await _payoutRepository.GetByStoreIdAsync(query.StoreId, cancellationToken);

        // Get pending eligible allocations not yet in a payout
        var eligibleAllocations = await _escrowRepository.GetEligibleForPayoutByStoreIdAsync(
            query.StoreId, cancellationToken);

        decimal pendingAmount = 0m;
        foreach (var allocation in eligibleAllocations)
        {
            var isInPayout = await _payoutRepository.IsAllocationInPayoutAsync(allocation.Id, cancellationToken);
            if (!isInPayout)
            {
                pendingAmount += allocation.GetRemainingSellerPayout();
            }
        }

        var scheduledPayouts = payouts.Where(p => p.Status == SellerPayoutStatus.Scheduled).ToList();
        var processingPayouts = payouts.Where(p => p.Status == SellerPayoutStatus.Processing).ToList();
        var failedPayouts = payouts.Where(p => p.Status == SellerPayoutStatus.Failed).ToList();

        var currency = eligibleAllocations.FirstOrDefault()?.Currency ?? payouts.FirstOrDefault()?.Currency ?? DefaultCurrency;

        return new SellerPayoutSummaryDto(
            query.StoreId,
            pendingAmount,
            scheduledPayouts.Sum(p => p.TotalAmount),
            processingPayouts.Sum(p => p.TotalAmount),
            scheduledPayouts.Count,
            failedPayouts.Count,
            scheduledPayouts.OrderBy(p => p.ScheduledDate).FirstOrDefault()?.ScheduledDate,
            currency);
    }

    /// <summary>
    /// Gets payouts by status.
    /// </summary>
    public async Task<IReadOnlyList<SellerPayoutDto>> HandleAsync(
        GetPayoutsByStatusQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!Enum.TryParse<SellerPayoutStatus>(query.Status, true, out var status))
        {
            return Array.Empty<SellerPayoutDto>();
        }

        var payouts = await _payoutRepository.GetByStatusAsync(status, cancellationToken);
        return payouts.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Calculates the next payout date based on frequency and day of week.
    /// </summary>
    private static DateTime CalculateNextPayoutDate(PayoutScheduleFrequency frequency, DayOfWeek payoutDay)
    {
        var today = DateTime.UtcNow.Date;
        var daysUntilPayoutDay = ((int)payoutDay - (int)today.DayOfWeek + 7) % 7;

        // If today is the payout day, schedule for next week
        if (daysUntilPayoutDay == 0)
        {
            daysUntilPayoutDay = 7;
        }

        var nextPayoutDate = today.AddDays(daysUntilPayoutDay);

        return frequency switch
        {
            PayoutScheduleFrequency.BiWeekly => nextPayoutDate.AddDays(7),
            PayoutScheduleFrequency.Monthly => GetNextMonthPayoutDate(today, payoutDay),
            _ => nextPayoutDate // Weekly
        };
    }

    /// <summary>
    /// Gets the next monthly payout date (first occurrence of the day of week in next month).
    /// </summary>
    private static DateTime GetNextMonthPayoutDate(DateTime today, DayOfWeek payoutDay)
    {
        var firstOfNextMonth = new DateTime(today.Year, today.Month, 1).AddMonths(1);
        var daysUntilPayoutDay = ((int)payoutDay - (int)firstOfNextMonth.DayOfWeek + 7) % 7;
        return firstOfNextMonth.AddDays(daysUntilPayoutDay);
    }

    /// <summary>
    /// Generates a payout reference for tracking purposes.
    /// Format: PO-{timestamp}-{payoutId first 8 chars}
    /// </summary>
    private static string GeneratePayoutReference(Guid payoutId)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var shortId = payoutId.ToString("N")[..8].ToUpperInvariant();
        return $"PO-{timestamp}-{shortId}";
    }

    private static SellerPayoutDto MapToDto(SellerPayout payout)
    {
        return new SellerPayoutDto(
            payout.Id,
            payout.StoreId,
            payout.SellerId,
            payout.TotalAmount,
            payout.Currency,
            payout.Status.ToString(),
            payout.ScheduledDate,
            payout.PayoutMethod.ToString(),
            payout.PayoutReference,
            payout.ErrorReference,
            payout.ErrorMessage,
            payout.RetryCount,
            payout.MaxRetries,
            payout.Items.Select(MapItemToDto).ToList(),
            payout.CreatedAt,
            payout.ProcessedAt,
            payout.PaidAt,
            payout.FailedAt,
            payout.NextRetryAt);
    }

    private static SellerPayoutItemDto MapItemToDto(SellerPayoutItem item)
    {
        return new SellerPayoutItemDto(
            item.Id,
            item.EscrowAllocationId,
            item.Amount,
            item.CreatedAt);
    }
}
