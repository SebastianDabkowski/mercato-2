namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the status of a seller payout.
/// </summary>
public enum SellerPayoutStatus
{
    /// <summary>Payout is scheduled for processing.</summary>
    Scheduled,
    /// <summary>Payout is being processed.</summary>
    Processing,
    /// <summary>Payout has been successfully completed.</summary>
    Paid,
    /// <summary>Payout failed and may be retried.</summary>
    Failed
}

/// <summary>
/// Represents the payout schedule frequency.
/// </summary>
public enum PayoutScheduleFrequency
{
    /// <summary>Payouts are processed weekly.</summary>
    Weekly,
    /// <summary>Payouts are processed bi-weekly.</summary>
    BiWeekly,
    /// <summary>Payouts are processed monthly.</summary>
    Monthly
}

/// <summary>
/// Represents a scheduled payout to a seller.
/// Aggregates eligible escrow allocations into a single payout batch.
/// </summary>
public class SellerPayout
{
    private readonly List<SellerPayoutItem> _items = new();

    public Guid Id { get; private set; }

    /// <summary>
    /// The store (seller) receiving the payout.
    /// </summary>
    public Guid StoreId { get; private set; }

    /// <summary>
    /// The seller's user ID (for payout settings lookup).
    /// </summary>
    public Guid SellerId { get; private set; }

    /// <summary>
    /// Total amount to be paid out to the seller.
    /// </summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>
    /// Currency code for the payout.
    /// </summary>
    public string Currency { get; private set; } = default!;

    /// <summary>
    /// Current status of the payout.
    /// </summary>
    public SellerPayoutStatus Status { get; private set; }

    /// <summary>
    /// The date when this payout is scheduled to be processed.
    /// </summary>
    public DateTime ScheduledDate { get; private set; }

    /// <summary>
    /// The payout method used (BankTransfer, Sepa, etc.).
    /// </summary>
    public PayoutMethod PayoutMethod { get; private set; }

    /// <summary>
    /// External reference ID from the payment provider.
    /// </summary>
    public string? PayoutReference { get; private set; }

    /// <summary>
    /// Error reference if the payout failed.
    /// </summary>
    public string? ErrorReference { get; private set; }

    /// <summary>
    /// Error message describing the failure.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Number of retry attempts made.
    /// </summary>
    public int RetryCount { get; private set; }

    /// <summary>
    /// Maximum number of retries allowed.
    /// </summary>
    public int MaxRetries { get; private set; }

    /// <summary>
    /// Items included in this payout.
    /// </summary>
    public IReadOnlyCollection<SellerPayoutItem> Items => _items.AsReadOnly();

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public DateTime? FailedAt { get; private set; }
    public DateTime? NextRetryAt { get; private set; }

    private SellerPayout()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new seller payout.
    /// </summary>
    public SellerPayout(
        Guid storeId,
        Guid sellerId,
        string currency,
        DateTime scheduledDate,
        PayoutMethod payoutMethod,
        int maxRetries = 3)
    {
        if (storeId == Guid.Empty)
        {
            throw new ArgumentException("Store ID is required.", nameof(storeId));
        }

        if (sellerId == Guid.Empty)
        {
            throw new ArgumentException("Seller ID is required.", nameof(sellerId));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        if (payoutMethod == PayoutMethod.None)
        {
            throw new ArgumentException("A valid payout method is required.", nameof(payoutMethod));
        }

        Id = Guid.NewGuid();
        StoreId = storeId;
        SellerId = sellerId;
        Currency = currency.ToUpperInvariant();
        TotalAmount = 0m;
        Status = SellerPayoutStatus.Scheduled;
        ScheduledDate = scheduledDate;
        PayoutMethod = payoutMethod;
        RetryCount = 0;
        MaxRetries = maxRetries;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds an escrow allocation to this payout.
    /// </summary>
    public SellerPayoutItem AddItem(EscrowAllocation allocation)
    {
        ArgumentNullException.ThrowIfNull(allocation);

        if (Status != SellerPayoutStatus.Scheduled)
        {
            throw new InvalidOperationException($"Cannot add items to payout in status {Status}.");
        }

        if (allocation.Status != EscrowAllocationStatus.Held)
        {
            throw new InvalidOperationException("Only held allocations can be added to a payout.");
        }

        if (!allocation.IsEligibleForPayout)
        {
            throw new InvalidOperationException("Allocation is not eligible for payout.");
        }

        if (allocation.Currency != Currency)
        {
            throw new InvalidOperationException($"Allocation currency {allocation.Currency} does not match payout currency {Currency}.");
        }

        var existingItem = _items.FirstOrDefault(i => i.EscrowAllocationId == allocation.Id);
        if (existingItem is not null)
        {
            throw new InvalidOperationException($"Allocation {allocation.Id} is already included in this payout.");
        }

        var item = new SellerPayoutItem(Id, allocation.Id, allocation.GetRemainingSellerPayout());
        _items.Add(item);

        TotalAmount = _items.Sum(i => i.Amount);
        UpdatedAt = DateTime.UtcNow;

        return item;
    }

    /// <summary>
    /// Starts processing the payout.
    /// </summary>
    public void StartProcessing()
    {
        if (Status != SellerPayoutStatus.Scheduled && Status != SellerPayoutStatus.Failed)
        {
            throw new InvalidOperationException($"Cannot start processing payout in status {Status}.");
        }

        if (_items.Count == 0)
        {
            throw new InvalidOperationException("Cannot process a payout with no items.");
        }

        Status = SellerPayoutStatus.Processing;
        ProcessedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the payout as successfully completed.
    /// </summary>
    public void MarkPaid(string? payoutReference = null)
    {
        if (Status != SellerPayoutStatus.Processing)
        {
            throw new InvalidOperationException($"Cannot mark payout as paid in status {Status}.");
        }

        Status = SellerPayoutStatus.Paid;
        PayoutReference = payoutReference;
        PaidAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        ErrorReference = null;
        ErrorMessage = null;
    }

    /// <summary>
    /// Marks the payout as failed.
    /// </summary>
    public void MarkFailed(string? errorReference, string? errorMessage)
    {
        if (Status != SellerPayoutStatus.Processing)
        {
            throw new InvalidOperationException($"Cannot mark payout as failed in status {Status}.");
        }

        Status = SellerPayoutStatus.Failed;
        ErrorReference = errorReference;
        ErrorMessage = errorMessage;
        FailedAt = DateTime.UtcNow;
        RetryCount++;
        UpdatedAt = DateTime.UtcNow;

        // Schedule next retry if retries remaining
        if (RetryCount < MaxRetries)
        {
            // Exponential backoff: 1 hour, 4 hours, 16 hours
            var delayHours = Math.Pow(4, RetryCount);
            NextRetryAt = DateTime.UtcNow.AddHours(delayHours);
        }
        else
        {
            NextRetryAt = null;
        }
    }

    /// <summary>
    /// Checks if the payout can be retried.
    /// </summary>
    public bool CanRetry()
    {
        return Status == SellerPayoutStatus.Failed && RetryCount < MaxRetries;
    }

    /// <summary>
    /// Checks if the payout is due for retry.
    /// </summary>
    public bool IsDueForRetry()
    {
        return CanRetry() && NextRetryAt.HasValue && DateTime.UtcNow >= NextRetryAt.Value;
    }

    /// <summary>
    /// Loads items from persistence.
    /// </summary>
    public void LoadItems(IEnumerable<SellerPayoutItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
        TotalAmount = _items.Sum(i => i.Amount);
    }
}
