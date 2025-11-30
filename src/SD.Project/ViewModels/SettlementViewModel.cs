namespace SD.Project.ViewModels;

/// <summary>
/// View model for settlement list item.
/// </summary>
public sealed record SettlementListItemViewModel(
    Guid Id,
    Guid StoreId,
    string StoreName,
    int Year,
    int Month,
    string SettlementNumber,
    string Status,
    string Currency,
    decimal NetPayable,
    int OrderCount,
    int Version,
    DateTime CreatedAt,
    DateTime? FinalizedAt)
{
    /// <summary>
    /// Gets the period display string (e.g., "December 2024").
    /// </summary>
    public string PeriodDisplay => new DateTime(Year, Month, 1).ToString("MMMM yyyy");

    /// <summary>
    /// Gets the status badge CSS class.
    /// </summary>
    public string StatusBadgeClass => Status switch
    {
        "Draft" => "bg-secondary",
        "Finalized" => "bg-info",
        "Approved" => "bg-success",
        "Exported" => "bg-primary",
        _ => "bg-secondary"
    };
}

/// <summary>
/// View model for detailed settlement view.
/// </summary>
public sealed record SettlementDetailsViewModel(
    Guid Id,
    Guid StoreId,
    Guid SellerId,
    string StoreName,
    int Year,
    int Month,
    string SettlementNumber,
    string Status,
    string Currency,
    decimal GrossSales,
    decimal TotalShipping,
    decimal TotalCommission,
    decimal TotalRefunds,
    decimal TotalAdjustments,
    decimal NetPayable,
    int OrderCount,
    int Version,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    IReadOnlyList<SettlementItemViewModel> Items,
    IReadOnlyList<SettlementAdjustmentViewModel> Adjustments,
    DateTime CreatedAt,
    DateTime? FinalizedAt,
    DateTime? ApprovedAt,
    DateTime? ExportedAt,
    string? ApprovedBy,
    string? Notes)
{
    /// <summary>
    /// Gets the period display string (e.g., "December 2024").
    /// </summary>
    public string PeriodDisplay => new DateTime(Year, Month, 1).ToString("MMMM yyyy");

    /// <summary>
    /// Gets the status badge CSS class.
    /// </summary>
    public string StatusBadgeClass => Status switch
    {
        "Draft" => "bg-secondary",
        "Finalized" => "bg-info",
        "Approved" => "bg-success",
        "Exported" => "bg-primary",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Checks if the settlement can be finalized.
    /// </summary>
    public bool CanFinalize => Status == "Draft";

    /// <summary>
    /// Checks if the settlement can be approved.
    /// </summary>
    public bool CanApprove => Status == "Finalized";

    /// <summary>
    /// Checks if the settlement can be exported.
    /// </summary>
    public bool CanExport => Status == "Finalized" || Status == "Approved";
}

/// <summary>
/// View model for settlement item.
/// </summary>
public sealed record SettlementItemViewModel(
    Guid Id,
    Guid EscrowAllocationId,
    Guid ShipmentId,
    string? OrderNumber,
    decimal SellerAmount,
    decimal ShippingAmount,
    decimal CommissionAmount,
    decimal RefundedAmount,
    decimal NetAmount,
    DateTime TransactionDate);

/// <summary>
/// View model for settlement adjustment.
/// </summary>
public sealed record SettlementAdjustmentViewModel(
    Guid Id,
    int OriginalYear,
    int OriginalMonth,
    decimal Amount,
    string Reason,
    Guid? RelatedOrderId,
    string? RelatedOrderNumber,
    DateTime CreatedAt)
{
    /// <summary>
    /// Gets the original period display string.
    /// </summary>
    public string OriginalPeriodDisplay => new DateTime(OriginalYear, OriginalMonth, 1).ToString("MMMM yyyy");

    /// <summary>
    /// Gets the amount display class (green for positive, red for negative).
    /// </summary>
    public string AmountClass => Amount >= 0 ? "text-success" : "text-danger";
}

/// <summary>
/// View model for settlements summary.
/// </summary>
public sealed record SettlementsSummaryViewModel(
    int TotalSettlements,
    int DraftCount,
    int FinalizedCount,
    int ApprovedCount,
    int ExportedCount,
    decimal TotalNetPayable,
    string Currency);
