namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get filtered admin order/revenue report data.
/// </summary>
public sealed record GetAdminOrderReportQuery(
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    Guid? SellerId = null,
    string? OrderStatus = null,
    string? PaymentStatus = null,
    int PageNumber = 1,
    int PageSize = 20);

/// <summary>
/// Query to export admin order/revenue report data to CSV.
/// </summary>
public sealed record ExportAdminOrderReportQuery(
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    Guid? SellerId = null,
    string? OrderStatus = null,
    string? PaymentStatus = null);
