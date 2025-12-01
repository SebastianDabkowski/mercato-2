namespace SD.Project.ViewModels;

/// <summary>
/// View model for a single row in the seller order/revenue report.
/// </summary>
public sealed class SellerOrderReportRowViewModel
{
    public Guid OrderId { get; init; }
    public Guid SubOrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public DateTime OrderDate { get; init; }
    public string BuyerName { get; init; } = string.Empty;
    public string OrderStatus { get; init; } = string.Empty;
    public string PaymentStatus { get; init; } = string.Empty;
    public decimal OrderValue { get; init; }
    public decimal Commission { get; init; }
    public decimal NetAmount { get; init; }
    public string Currency { get; init; } = "PLN";

    /// <summary>
    /// Gets the formatted order value with currency.
    /// </summary>
    public string FormattedOrderValue => $"{Currency} {OrderValue:N2}";

    /// <summary>
    /// Gets the formatted commission with currency.
    /// </summary>
    public string FormattedCommission => $"{Currency} {Commission:N2}";

    /// <summary>
    /// Gets the formatted net amount with currency.
    /// </summary>
    public string FormattedNetAmount => $"{Currency} {NetAmount:N2}";

    /// <summary>
    /// Gets the formatted order date.
    /// </summary>
    public string FormattedOrderDate => OrderDate.ToString("yyyy-MM-dd HH:mm");

    /// <summary>
    /// Gets the CSS class for order status badge.
    /// </summary>
    public string OrderStatusBadgeClass => OrderStatus switch
    {
        "Pending" => "bg-warning",
        "Paid" => "bg-success",
        "Processing" => "bg-info",
        "Shipped" => "bg-primary",
        "Delivered" => "bg-success",
        "Cancelled" => "bg-secondary",
        "Refunded" => "bg-dark",
        _ => "bg-secondary"
    };

    /// <summary>
    /// Gets the CSS class for payment status badge.
    /// </summary>
    public string PaymentStatusBadgeClass => PaymentStatus switch
    {
        "Pending" => "bg-warning",
        "Paid" => "bg-success",
        "Failed" => "bg-danger",
        "Refunded" => "bg-dark",
        _ => "bg-secondary"
    };
}

/// <summary>
/// View model for the seller order/revenue report page.
/// </summary>
public sealed class SellerOrderReportViewModel
{
    public IReadOnlyList<SellerOrderReportRowViewModel> Rows { get; init; } = Array.Empty<SellerOrderReportRowViewModel>();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
    public decimal TotalOrderValue { get; init; }
    public decimal TotalCommission { get; init; }
    public decimal TotalNetAmount { get; init; }
    public string Currency { get; init; } = "PLN";
    public bool ExceedsExportThreshold { get; init; }

    /// <summary>
    /// Gets the formatted total order value with currency.
    /// </summary>
    public string FormattedTotalOrderValue => $"{Currency} {TotalOrderValue:N2}";

    /// <summary>
    /// Gets the formatted total commission with currency.
    /// </summary>
    public string FormattedTotalCommission => $"{Currency} {TotalCommission:N2}";

    /// <summary>
    /// Gets the formatted total net amount with currency.
    /// </summary>
    public string FormattedTotalNetAmount => $"{Currency} {TotalNetAmount:N2}";
}
