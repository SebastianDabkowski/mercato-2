using System.Globalization;
using System.Text;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for commission summaries.
/// Provides aggregated commission data per seller for admin reporting.
/// </summary>
public sealed class CommissionSummaryService
{
    private readonly IEscrowRepository _escrowRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IOrderRepository _orderRepository;

    /// <summary>
    /// Default currency when none is available.
    /// </summary>
    public const string DefaultCurrency = "EUR";

    public CommissionSummaryService(
        IEscrowRepository escrowRepository,
        IStoreRepository storeRepository,
        IOrderRepository orderRepository)
    {
        ArgumentNullException.ThrowIfNull(escrowRepository);
        ArgumentNullException.ThrowIfNull(storeRepository);
        ArgumentNullException.ThrowIfNull(orderRepository);

        _escrowRepository = escrowRepository;
        _storeRepository = storeRepository;
        _orderRepository = orderRepository;
    }

    /// <summary>
    /// Gets commission summaries grouped by seller for a date range.
    /// Uses historical commission data stored with each order (escrow allocation).
    /// </summary>
    public async Task<CommissionSummaryResultDto> HandleAsync(
        GetCommissionSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Normalize date range to full days
        var fromDate = query.FromDate.Date;
        var toDate = query.ToDate.Date.AddDays(1).AddTicks(-1);

        // Get all escrow allocations for the period
        var allocations = await _escrowRepository.GetAllocationsByDateRangeAsync(
            fromDate, toDate, cancellationToken);

        if (allocations.Count == 0)
        {
            return new CommissionSummaryResultDto(
                Array.Empty<CommissionSummaryDto>(),
                0m, 0m, 0m,
                DefaultCurrency,
                fromDate,
                toDate);
        }

        // Get store names
        var storeIds = allocations.Select(a => a.StoreId).Distinct().ToList();
        var stores = await _storeRepository.GetByIdsAsync(storeIds, cancellationToken);
        var storeNames = stores.ToDictionary(s => s.Id, s => s.Name);

        // Determine currency from allocations
        var currency = allocations.FirstOrDefault()?.Currency ?? DefaultCurrency;

        // Group by seller and calculate aggregates
        // Uses historical commission data persisted per order, not recalculated
        var summaries = allocations
            .GroupBy(a => a.StoreId)
            .Select(g =>
            {
                // Count unique shipments as orders
                var orderCount = g.Select(a => a.ShipmentId).Distinct().Count();
                
                // GMV = Total seller amount + shipping (gross merchandise value)
                var totalGmv = g.Sum(a => a.TotalAmount - a.RefundedAmount);
                
                // Commission = persisted commission amount from historical rates
                var totalCommission = g.Sum(a => a.CommissionAmount - a.RefundedCommissionAmount);
                
                // Net payout = what seller receives (seller amount - commission + shipping - refunds)
                var totalNetPayout = g.Sum(a => a.GetRemainingSellerPayout());

                return new CommissionSummaryDto(
                    g.Key,
                    storeNames.GetValueOrDefault(g.Key, "Unknown Store"),
                    orderCount,
                    totalGmv,
                    totalCommission,
                    totalNetPayout,
                    currency);
            })
            .OrderByDescending(s => s.TotalGmv)
            .ToList();

        // Calculate totals
        var totalGmv = summaries.Sum(s => s.TotalGmv);
        var totalCommission = summaries.Sum(s => s.TotalCommission);
        var totalNetPayout = summaries.Sum(s => s.TotalNetPayout);

        return new CommissionSummaryResultDto(
            summaries,
            totalGmv,
            totalCommission,
            totalNetPayout,
            currency,
            fromDate,
            toDate);
    }

    /// <summary>
    /// Gets drill-down order details for a specific seller's commission summary.
    /// Shows individual orders contributing to the seller's totals.
    /// </summary>
    public async Task<CommissionDrillDownResultDto?> HandleAsync(
        GetCommissionDrillDownQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Validate store exists
        var store = await _storeRepository.GetByIdAsync(query.StoreId, cancellationToken);
        if (store is null)
        {
            return null;
        }

        // Normalize date range to full days
        var fromDate = query.FromDate.Date;
        var toDate = query.ToDate.Date.AddDays(1).AddTicks(-1);

        // Get allocations for the store in the period
        var allocations = await _escrowRepository.GetAllocationsByStoreIdAndDateRangeAsync(
            query.StoreId, fromDate, toDate, cancellationToken);

        if (allocations.Count == 0)
        {
            return new CommissionDrillDownResultDto(
                query.StoreId,
                store.Name,
                Array.Empty<CommissionOrderDetailDto>(),
                0m, 0m, 0m,
                DefaultCurrency,
                fromDate,
                toDate);
        }

        var currency = allocations.FirstOrDefault()?.Currency ?? DefaultCurrency;

        // Get order numbers for shipments
        var orderDetails = new List<CommissionOrderDetailDto>();
        foreach (var allocation in allocations)
        {
            var (_, order, _) = await _orderRepository.GetShipmentWithOrderAsync(
                allocation.ShipmentId, cancellationToken);

            var gmvAmount = allocation.TotalAmount - allocation.RefundedAmount;
            var commissionAmount = allocation.CommissionAmount - allocation.RefundedCommissionAmount;
            var netPayout = allocation.GetRemainingSellerPayout();

            orderDetails.Add(new CommissionOrderDetailDto(
                allocation.Id,
                allocation.ShipmentId,
                order?.OrderNumber,
                allocation.CreatedAt,
                gmvAmount,
                commissionAmount,
                allocation.CommissionRate,
                netPayout,
                allocation.RefundedAmount,
                currency));
        }

        // Calculate totals
        var totalGmv = orderDetails.Sum(o => o.GmvAmount);
        var totalCommission = orderDetails.Sum(o => o.CommissionAmount);
        var totalNetPayout = orderDetails.Sum(o => o.NetPayout);

        return new CommissionDrillDownResultDto(
            query.StoreId,
            store.Name,
            orderDetails,
            totalGmv,
            totalCommission,
            totalNetPayout,
            currency,
            fromDate,
            toDate);
    }

    /// <summary>
    /// Exports commission summaries to CSV format.
    /// One row per seller with aggregated values for the selected period.
    /// </summary>
    public async Task<ExportResultDto> HandleAsync(
        ExportCommissionSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Get summary data
        var summaryQuery = new GetCommissionSummaryQuery(query.FromDate, query.ToDate);
        var result = await HandleAsync(summaryQuery, cancellationToken);

        if (result.Summaries.Count == 0)
        {
            return ExportResultDto.Failed("No commission data found for the selected period.");
        }

        // Generate CSV content
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        return GenerateCsvExport(result, timestamp);
    }

    private static ExportResultDto GenerateCsvExport(CommissionSummaryResultDto result, string timestamp)
    {
        var sb = new StringBuilder();

        // UTF-8 BOM for Excel compatibility
        sb.Append('\uFEFF');

        // Header row
        sb.AppendLine("Store Name,Order Count,Total GMV,Total Commission,Total Net Payout,Currency");

        // Data rows
        foreach (var summary in result.Summaries)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsvValue(summary.StoreName),
                summary.OrderCount.ToString(CultureInfo.InvariantCulture),
                summary.TotalGmv.ToString("F2", CultureInfo.InvariantCulture),
                summary.TotalCommission.ToString("F2", CultureInfo.InvariantCulture),
                summary.TotalNetPayout.ToString("F2", CultureInfo.InvariantCulture),
                EscapeCsvValue(summary.Currency)));
        }

        // Totals row
        sb.AppendLine();
        sb.AppendLine(string.Join(",",
            EscapeCsvValue("TOTAL"),
            result.Summaries.Sum(s => s.OrderCount).ToString(CultureInfo.InvariantCulture),
            result.TotalGmv.ToString("F2", CultureInfo.InvariantCulture),
            result.TotalCommission.ToString("F2", CultureInfo.InvariantCulture),
            result.TotalNetPayout.ToString("F2", CultureInfo.InvariantCulture),
            EscapeCsvValue(result.Currency)));

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fromDateStr = result.FromDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var toDateStr = result.ToDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var fileName = $"commission-summary-{fromDateStr}-{toDateStr}-{timestamp}.csv";

        return ExportResultDto.Success(bytes, fileName, "text/csv", result.Summaries.Count);
    }

    private static string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            value = value.Replace("\"", "\"\"");
            return $"\"{value}\"";
        }

        return value;
    }
}
