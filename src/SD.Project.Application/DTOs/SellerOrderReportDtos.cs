namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO representing a single row in the seller order/revenue report.
/// Includes financial fields for sales reconciliation: order value, commission, and net amount.
/// </summary>
public sealed record SellerOrderReportRowDto(
    Guid OrderId,
    Guid SubOrderId,
    string OrderNumber,
    DateTime OrderDate,
    string BuyerName,
    string OrderStatus,
    string PaymentStatus,
    decimal OrderValue,
    decimal Commission,
    decimal NetAmount,
    string Currency);

/// <summary>
/// DTO representing the result of the seller order/revenue report query.
/// </summary>
public sealed record SellerOrderReportResultDto(
    IReadOnlyList<SellerOrderReportRowDto> Rows,
    int TotalCount,
    int PageNumber,
    int PageSize,
    decimal TotalOrderValue,
    decimal TotalCommission,
    decimal TotalNetAmount,
    string Currency,
    bool ExceedsExportThreshold);
