namespace SD.Project.ViewModels;

/// <summary>
/// View model for commission summary per seller.
/// </summary>
public sealed record CommissionSummaryRowViewModel(
    Guid StoreId,
    string StoreName,
    int OrderCount,
    decimal TotalGmv,
    decimal TotalCommission,
    decimal TotalNetPayout,
    string Currency)
{
    /// <summary>
    /// Gets the formatted total GMV with currency.
    /// </summary>
    public string FormattedTotalGmv => $"{Currency} {TotalGmv:N2}";

    /// <summary>
    /// Gets the formatted total commission with currency.
    /// </summary>
    public string FormattedTotalCommission => $"{Currency} {TotalCommission:N2}";

    /// <summary>
    /// Gets the formatted total net payout with currency.
    /// </summary>
    public string FormattedTotalNetPayout => $"{Currency} {TotalNetPayout:N2}";
}

/// <summary>
/// View model for the commission summary page.
/// </summary>
public sealed class CommissionSummaryViewModel
{
    public IReadOnlyList<CommissionSummaryRowViewModel> Rows { get; init; } = Array.Empty<CommissionSummaryRowViewModel>();
    public decimal TotalGmv { get; init; }
    public decimal TotalCommission { get; init; }
    public decimal TotalNetPayout { get; init; }
    public string Currency { get; init; } = "EUR";
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }

    /// <summary>
    /// Gets the formatted total GMV with currency.
    /// </summary>
    public string FormattedTotalGmv => $"{Currency} {TotalGmv:N2}";

    /// <summary>
    /// Gets the formatted total commission with currency.
    /// </summary>
    public string FormattedTotalCommission => $"{Currency} {TotalCommission:N2}";

    /// <summary>
    /// Gets the formatted total net payout with currency.
    /// </summary>
    public string FormattedTotalNetPayout => $"{Currency} {TotalNetPayout:N2}";

    /// <summary>
    /// Gets the formatted date range display.
    /// </summary>
    public string DateRangeDisplay => $"{FromDate:MMM d, yyyy} - {ToDate:MMM d, yyyy}";
}

/// <summary>
/// View model for drill-down order details.
/// </summary>
public sealed record CommissionOrderDetailViewModel(
    Guid AllocationId,
    Guid ShipmentId,
    string? OrderNumber,
    DateTime OrderDate,
    decimal GmvAmount,
    decimal CommissionAmount,
    decimal CommissionRate,
    decimal NetPayout,
    decimal RefundedAmount,
    string Currency)
{
    /// <summary>
    /// Gets the formatted order date.
    /// </summary>
    public string FormattedOrderDate => OrderDate.ToString("MMM d, yyyy");

    /// <summary>
    /// Gets the formatted GMV amount with currency.
    /// </summary>
    public string FormattedGmvAmount => $"{Currency} {GmvAmount:N2}";

    /// <summary>
    /// Gets the formatted commission amount with currency.
    /// </summary>
    public string FormattedCommissionAmount => $"{Currency} {CommissionAmount:N2}";

    /// <summary>
    /// Gets the formatted commission rate as percentage.
    /// </summary>
    public string FormattedCommissionRate => $"{CommissionRate:N1}%";

    /// <summary>
    /// Gets the formatted net payout with currency.
    /// </summary>
    public string FormattedNetPayout => $"{Currency} {NetPayout:N2}";

    /// <summary>
    /// Gets the formatted refunded amount with currency.
    /// </summary>
    public string FormattedRefundedAmount => $"{Currency} {RefundedAmount:N2}";

    /// <summary>
    /// Indicates if there was a refund on this order.
    /// </summary>
    public bool HasRefund => RefundedAmount > 0;
}

/// <summary>
/// View model for the commission drill-down page.
/// </summary>
public sealed class CommissionDrillDownViewModel
{
    public Guid StoreId { get; init; }
    public string StoreName { get; init; } = string.Empty;
    public IReadOnlyList<CommissionOrderDetailViewModel> Orders { get; init; } = Array.Empty<CommissionOrderDetailViewModel>();
    public decimal TotalGmv { get; init; }
    public decimal TotalCommission { get; init; }
    public decimal TotalNetPayout { get; init; }
    public string Currency { get; init; } = "EUR";
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }

    /// <summary>
    /// Gets the formatted total GMV with currency.
    /// </summary>
    public string FormattedTotalGmv => $"{Currency} {TotalGmv:N2}";

    /// <summary>
    /// Gets the formatted total commission with currency.
    /// </summary>
    public string FormattedTotalCommission => $"{Currency} {TotalCommission:N2}";

    /// <summary>
    /// Gets the formatted total net payout with currency.
    /// </summary>
    public string FormattedTotalNetPayout => $"{Currency} {TotalNetPayout:N2}";

    /// <summary>
    /// Gets the formatted date range display.
    /// </summary>
    public string DateRangeDisplay => $"{FromDate:MMM d, yyyy} - {ToDate:MMM d, yyyy}";
}
