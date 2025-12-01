namespace SD.Project.ViewModels;

/// <summary>
/// View model for a single row in the admin order/revenue report.
/// </summary>
public sealed class OrderReportRowViewModel
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public DateTime OrderDate { get; init; }
    public string BuyerName { get; init; } = string.Empty;
    public string SellerName { get; init; } = string.Empty;
    public string OrderStatus { get; init; } = string.Empty;
    public string PaymentStatus { get; init; } = string.Empty;
    public decimal OrderValue { get; init; }
    public decimal Commission { get; init; }
    public decimal PayoutAmount { get; init; }
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
    /// Gets the formatted payout amount with currency.
    /// </summary>
    public string FormattedPayoutAmount => $"{Currency} {PayoutAmount:N2}";

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
        "PaymentConfirmed" => "bg-info",
        "Processing" => "bg-primary",
        "Shipped" => "bg-info",
        "Delivered" => "bg-success",
        "Cancelled" => "bg-secondary",
        "PaymentFailed" => "bg-danger",
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
/// View model for the admin order/revenue report page.
/// </summary>
public sealed class OrderReportViewModel
{
    public IReadOnlyList<OrderReportRowViewModel> Rows { get; init; } = Array.Empty<OrderReportRowViewModel>();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
    public decimal TotalOrderValue { get; init; }
    public decimal TotalCommission { get; init; }
    public decimal TotalPayoutAmount { get; init; }
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
    /// Gets the formatted total payout amount with currency.
    /// </summary>
    public string FormattedTotalPayoutAmount => $"{Currency} {TotalPayoutAmount:N2}";
}

/// <summary>
/// Represents a seller option for the seller filter dropdown.
/// </summary>
public sealed class SellerFilterOption
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsSelected { get; init; }
}
