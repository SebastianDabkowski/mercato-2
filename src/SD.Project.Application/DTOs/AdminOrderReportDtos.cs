namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO representing a single row in the admin order/revenue report.
/// Includes all key fields for marketplace performance analysis.
/// </summary>
public sealed record AdminOrderReportRowDto(
    Guid OrderId,
    string OrderNumber,
    DateTime OrderDate,
    Guid BuyerId,
    string BuyerName,
    Guid StoreId,
    string SellerName,
    string OrderStatus,
    string PaymentStatus,
    decimal OrderValue,
    decimal Commission,
    decimal PayoutAmount,
    string Currency);

/// <summary>
/// DTO representing the result of the admin order/revenue report query.
/// </summary>
public sealed record AdminOrderReportResultDto(
    IReadOnlyList<AdminOrderReportRowDto> Rows,
    int TotalCount,
    int PageNumber,
    int PageSize,
    decimal TotalOrderValue,
    decimal TotalCommission,
    decimal TotalPayoutAmount,
    string Currency,
    bool ExceedsExportThreshold);
