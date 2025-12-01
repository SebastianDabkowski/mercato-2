namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get filtered seller order/revenue report data.
/// </summary>
public sealed record GetSellerOrderReportQuery(
    Guid StoreId,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? OrderStatus = null,
    int PageNumber = 1,
    int PageSize = 20);

/// <summary>
/// Query to export seller order/revenue report data to CSV.
/// </summary>
public sealed record ExportSellerOrderReportQuery(
    Guid StoreId,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? OrderStatus = null);
