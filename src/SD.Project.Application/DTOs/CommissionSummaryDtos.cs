namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO for commission summary per seller.
/// </summary>
public sealed record CommissionSummaryDto(
    Guid StoreId,
    string StoreName,
    int OrderCount,
    decimal TotalGmv,
    decimal TotalCommission,
    decimal TotalNetPayout,
    string Currency);

/// <summary>
/// DTO for commission summary result including totals.
/// </summary>
public sealed record CommissionSummaryResultDto(
    IReadOnlyList<CommissionSummaryDto> Summaries,
    decimal TotalGmv,
    decimal TotalCommission,
    decimal TotalNetPayout,
    string Currency,
    DateTime FromDate,
    DateTime ToDate);

/// <summary>
/// DTO for drill-down order details within a commission summary.
/// </summary>
public sealed record CommissionOrderDetailDto(
    Guid AllocationId,
    Guid ShipmentId,
    string? OrderNumber,
    DateTime OrderDate,
    decimal GmvAmount,
    decimal CommissionAmount,
    decimal CommissionRate,
    decimal NetPayout,
    decimal RefundedAmount,
    string Currency);

/// <summary>
/// DTO for drill-down result with order details for a specific seller.
/// </summary>
public sealed record CommissionDrillDownResultDto(
    Guid StoreId,
    string StoreName,
    IReadOnlyList<CommissionOrderDetailDto> Orders,
    decimal TotalGmv,
    decimal TotalCommission,
    decimal TotalNetPayout,
    string Currency,
    DateTime FromDate,
    DateTime ToDate);

/// <summary>
/// DTO for CSV export row.
/// </summary>
public sealed record CommissionSummaryExportDto(
    string StoreName,
    int OrderCount,
    decimal TotalGmv,
    decimal TotalCommission,
    decimal TotalNetPayout,
    string Currency);
