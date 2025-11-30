namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the status of a monthly settlement.
/// </summary>
public enum SettlementStatus
{
    /// <summary>Settlement is being generated and can still be modified.</summary>
    Draft,
    /// <summary>Settlement has been finalized and is ready for review.</summary>
    Finalized,
    /// <summary>Settlement has been approved by finance.</summary>
    Approved,
    /// <summary>Settlement has been exported/downloaded.</summary>
    Exported
}

/// <summary>
/// Represents a monthly settlement report for a seller.
/// Aggregates all payouts and order data for a specific month.
/// </summary>
public class Settlement
{
    private readonly List<SettlementItem> _items = new();
    private readonly List<SettlementAdjustment> _adjustments = new();

    public Guid Id { get; private set; }

    /// <summary>
    /// The store (seller) this settlement is for.
    /// </summary>
    public Guid StoreId { get; private set; }

    /// <summary>
    /// The seller's user ID.
    /// </summary>
    public Guid SellerId { get; private set; }

    /// <summary>
    /// The year of this settlement period.
    /// </summary>
    public int Year { get; private set; }

    /// <summary>
    /// The month of this settlement period (1-12).
    /// </summary>
    public int Month { get; private set; }

    /// <summary>
    /// Human-readable settlement reference number.
    /// Format: STL-{StoreId first 4 chars}-{YYYYMM}-{version}
    /// </summary>
    public string SettlementNumber { get; private set; } = default!;

    /// <summary>
    /// Current status of the settlement.
    /// </summary>
    public SettlementStatus Status { get; private set; }

    /// <summary>
    /// Currency code for all amounts.
    /// </summary>
    public string Currency { get; private set; } = default!;

    /// <summary>
    /// Total gross sales amount (before commissions).
    /// </summary>
    public decimal GrossSales { get; private set; }

    /// <summary>
    /// Total shipping amount collected.
    /// </summary>
    public decimal TotalShipping { get; private set; }

    /// <summary>
    /// Total platform commission amount.
    /// </summary>
    public decimal TotalCommission { get; private set; }

    /// <summary>
    /// Total refunded amount during this period.
    /// </summary>
    public decimal TotalRefunds { get; private set; }

    /// <summary>
    /// Total adjustments from previous periods.
    /// </summary>
    public decimal TotalAdjustments { get; private set; }

    /// <summary>
    /// Net amount payable to the seller.
    /// Calculated as: GrossSales + TotalShipping - TotalCommission - TotalRefunds + TotalAdjustments
    /// </summary>
    public decimal NetPayable { get; private set; }

    /// <summary>
    /// Number of orders included in this settlement.
    /// </summary>
    public int OrderCount { get; private set; }

    /// <summary>
    /// Version number for regeneration tracking.
    /// Incremented each time the settlement is regenerated.
    /// </summary>
    public int Version { get; private set; }

    /// <summary>
    /// Start date of the settlement period.
    /// </summary>
    public DateTime PeriodStart { get; private set; }

    /// <summary>
    /// End date of the settlement period.
    /// </summary>
    public DateTime PeriodEnd { get; private set; }

    /// <summary>
    /// Items included in this settlement.
    /// </summary>
    public IReadOnlyCollection<SettlementItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Adjustments from previous periods.
    /// </summary>
    public IReadOnlyCollection<SettlementAdjustment> Adjustments => _adjustments.AsReadOnly();

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? FinalizedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? ExportedAt { get; private set; }
    public string? ApprovedBy { get; private set; }
    public string? Notes { get; private set; }

    private Settlement()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new settlement for a seller's monthly period.
    /// </summary>
    public Settlement(
        Guid storeId,
        Guid sellerId,
        int year,
        int month,
        string currency,
        int version = 1)
    {
        if (storeId == Guid.Empty)
        {
            throw new ArgumentException("Store ID is required.", nameof(storeId));
        }

        if (sellerId == Guid.Empty)
        {
            throw new ArgumentException("Seller ID is required.", nameof(sellerId));
        }

        if (year < 2020 || year > 2100)
        {
            throw new ArgumentException("Year must be between 2020 and 2100.", nameof(year));
        }

        if (month < 1 || month > 12)
        {
            throw new ArgumentException("Month must be between 1 and 12.", nameof(month));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        if (version < 1)
        {
            throw new ArgumentException("Version must be at least 1.", nameof(version));
        }

        Id = Guid.NewGuid();
        StoreId = storeId;
        SellerId = sellerId;
        Year = year;
        Month = month;
        Currency = currency.ToUpperInvariant();
        Status = SettlementStatus.Draft;
        Version = version;

        // Calculate period dates
        PeriodStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        PeriodEnd = PeriodStart.AddMonths(1).AddTicks(-1);

        SettlementNumber = GenerateSettlementNumber(storeId, year, month, version);

        GrossSales = 0m;
        TotalShipping = 0m;
        TotalCommission = 0m;
        TotalRefunds = 0m;
        TotalAdjustments = 0m;
        NetPayable = 0m;
        OrderCount = 0;

        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds a settlement item (escrow allocation) to this settlement.
    /// </summary>
    public SettlementItem AddItem(
        Guid escrowAllocationId,
        Guid shipmentId,
        string? orderNumber,
        decimal sellerAmount,
        decimal shippingAmount,
        decimal commissionAmount,
        decimal refundedAmount,
        DateTime transactionDate)
    {
        if (Status != SettlementStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot add items to settlement in status {Status}.");
        }

        var existingItem = _items.FirstOrDefault(i => i.EscrowAllocationId == escrowAllocationId);
        if (existingItem is not null)
        {
            throw new InvalidOperationException($"Allocation {escrowAllocationId} is already included in this settlement.");
        }

        var item = new SettlementItem(
            Id,
            escrowAllocationId,
            shipmentId,
            orderNumber,
            sellerAmount,
            shippingAmount,
            commissionAmount,
            refundedAmount,
            transactionDate);

        _items.Add(item);
        RecalculateTotals();
        UpdatedAt = DateTime.UtcNow;

        return item;
    }

    /// <summary>
    /// Adds an adjustment for a previous period.
    /// </summary>
    public SettlementAdjustment AddAdjustment(
        int originalYear,
        int originalMonth,
        decimal amount,
        string reason,
        Guid? relatedOrderId = null,
        string? relatedOrderNumber = null)
    {
        if (Status != SettlementStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot add adjustments to settlement in status {Status}.");
        }

        var adjustment = new SettlementAdjustment(
            Id,
            originalYear,
            originalMonth,
            amount,
            reason,
            relatedOrderId,
            relatedOrderNumber);

        _adjustments.Add(adjustment);
        RecalculateTotals();
        UpdatedAt = DateTime.UtcNow;

        return adjustment;
    }

    /// <summary>
    /// Finalizes the settlement, preventing further modifications.
    /// </summary>
    public void FinalizeSettlement()
    {
        if (Status != SettlementStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot finalize settlement in status {Status}.");
        }

        Status = SettlementStatus.Finalized;
        FinalizedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Approves the settlement.
    /// </summary>
    public void Approve(string approvedBy)
    {
        if (Status != SettlementStatus.Finalized)
        {
            throw new InvalidOperationException($"Cannot approve settlement in status {Status}.");
        }

        if (string.IsNullOrWhiteSpace(approvedBy))
        {
            throw new ArgumentException("Approved by is required.", nameof(approvedBy));
        }

        Status = SettlementStatus.Approved;
        ApprovedAt = DateTime.UtcNow;
        ApprovedBy = approvedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the settlement as exported.
    /// </summary>
    public void MarkExported()
    {
        if (Status != SettlementStatus.Approved && Status != SettlementStatus.Finalized)
        {
            throw new InvalidOperationException($"Cannot mark settlement as exported in status {Status}.");
        }

        Status = SettlementStatus.Exported;
        ExportedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the notes for this settlement.
    /// </summary>
    public void UpdateNotes(string? notes)
    {
        Notes = notes?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Clears all items for regeneration.
    /// Only allowed in Draft status.
    /// </summary>
    public void ClearItems()
    {
        if (Status != SettlementStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot clear items from settlement in status {Status}.");
        }

        _items.Clear();
        RecalculateTotals();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Clears all adjustments for regeneration.
    /// Only allowed in Draft status.
    /// </summary>
    public void ClearAdjustments()
    {
        if (Status != SettlementStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot clear adjustments from settlement in status {Status}.");
        }

        _adjustments.Clear();
        RecalculateTotals();
        UpdatedAt = DateTime.UtcNow;
    }

    private void RecalculateTotals()
    {
        GrossSales = _items.Sum(i => i.SellerAmount);
        TotalShipping = _items.Sum(i => i.ShippingAmount);
        TotalCommission = _items.Sum(i => i.CommissionAmount);
        TotalRefunds = _items.Sum(i => i.RefundedAmount);
        TotalAdjustments = _adjustments.Sum(a => a.Amount);
        OrderCount = _items.Select(i => i.OrderNumber).Distinct().Count();
        NetPayable = GrossSales + TotalShipping - TotalCommission - TotalRefunds + TotalAdjustments;
    }

    private static string GenerateSettlementNumber(Guid storeId, int year, int month, int version)
    {
        var storePrefix = storeId.ToString("N")[..4].ToUpperInvariant();
        return $"STL-{storePrefix}-{year}{month:D2}-V{version}";
    }

    /// <summary>
    /// Loads items from persistence.
    /// </summary>
    public void LoadItems(IEnumerable<SettlementItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
        RecalculateTotals();
    }

    /// <summary>
    /// Loads adjustments from persistence.
    /// </summary>
    public void LoadAdjustments(IEnumerable<SettlementAdjustment> adjustments)
    {
        _adjustments.Clear();
        _adjustments.AddRange(adjustments);
        RecalculateTotals();
    }
}
