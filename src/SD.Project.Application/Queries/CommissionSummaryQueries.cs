namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get commission summaries grouped by seller for a date range.
/// </summary>
public sealed record GetCommissionSummaryQuery(
    DateTime FromDate,
    DateTime ToDate);

/// <summary>
/// Query to get drill-down order details for a specific seller's commission summary.
/// </summary>
public sealed record GetCommissionDrillDownQuery(
    Guid StoreId,
    DateTime FromDate,
    DateTime ToDate);

/// <summary>
/// Query to export commission summaries to CSV.
/// </summary>
public sealed record ExportCommissionSummaryQuery(
    DateTime FromDate,
    DateTime ToDate);
