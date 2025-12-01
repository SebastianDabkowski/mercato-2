using System.Globalization;
using System.Text;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Domain.Services;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for seller order and revenue reports.
/// Provides filtered report data and CSV export for sellers to reconcile sales.
/// </summary>
public sealed class SellerOrderReportService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;
    private readonly CommissionCalculator _commissionCalculator;

    /// <summary>
    /// Maximum number of rows that can be exported without warning.
    /// Above this threshold, the system will warn about large exports.
    /// </summary>
    public const int ExportThreshold = 10000;

    public SellerOrderReportService(
        IOrderRepository orderRepository,
        IUserRepository userRepository,
        CommissionCalculator commissionCalculator)
    {
        ArgumentNullException.ThrowIfNull(orderRepository);
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(commissionCalculator);

        _orderRepository = orderRepository;
        _userRepository = userRepository;
        _commissionCalculator = commissionCalculator;
    }

    /// <summary>
    /// Gets filtered seller order/revenue report data with pagination.
    /// Shows only orders that belong to the seller's store.
    /// </summary>
    public async Task<SellerOrderReportResultDto> HandleAsync(
        GetSellerOrderReportQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Parse status filter
        ShipmentStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(query.OrderStatus) &&
            Enum.TryParse<ShipmentStatus>(query.OrderStatus, ignoreCase: true, out var parsedStatus))
        {
            statusFilter = parsedStatus;
        }

        // Validate and normalize pagination
        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var skip = (pageNumber - 1) * pageSize;

        // Get filtered shipments from repository
        var (shipments, totalCount) = await _orderRepository.GetFilteredShipmentsByStoreIdAsync(
            query.StoreId,
            statusFilter,
            query.FromDate,
            query.ToDate,
            null, // No buyer search for report
            null, // No tracking filter for report
            skip,
            pageSize,
            cancellationToken);

        if (shipments.Count == 0)
        {
            return new SellerOrderReportResultDto(
                Array.Empty<SellerOrderReportRowDto>(),
                0, pageNumber, pageSize,
                0m, 0m, 0m, "PLN",
                false);
        }

        // Get order details for all shipments
        var orderIds = shipments.Select(s => s.OrderId).Distinct().ToList();
        var orders = new Dictionary<Guid, Order>();
        var buyers = new Dictionary<Guid, User?>();

        foreach (var orderId in orderIds)
        {
            var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
            if (order is not null)
            {
                orders[orderId] = order;
                if (!buyers.ContainsKey(order.BuyerId))
                {
                    buyers[order.BuyerId] = await _userRepository.GetByIdAsync(order.BuyerId, cancellationToken);
                }
            }
        }

        // Build report rows
        var rows = new List<SellerOrderReportRowDto>();
        decimal totalOrderValue = 0m;
        decimal totalCommission = 0m;
        decimal totalNetAmount = 0m;
        string currency = "PLN";

        foreach (var shipment in shipments)
        {
            if (!orders.TryGetValue(shipment.OrderId, out var order))
            {
                continue;
            }

            var buyer = buyers.GetValueOrDefault(order.BuyerId);
            var buyerName = buyer?.FirstName is not null && buyer?.LastName is not null
                ? $"{buyer.FirstName} {buyer.LastName}"
                : order.RecipientName;

            currency = order.Currency;

            // Calculate commission for this shipment
            var commission = _commissionCalculator.CalculateCommission(
                shipment.StoreId,
                shipment.Subtotal,
                order.Currency);

            var orderValue = shipment.Subtotal + shipment.ShippingCost;
            var commissionAmount = commission.CommissionAmount.Amount;
            // Net amount = seller payout + shipping cost (commission is only on subtotal)
            var netAmount = commission.SellerPayout.Amount + shipment.ShippingCost;

            // Determine payment status from order
            var paymentStatus = order.Status switch
            {
                OrderStatus.Pending => "Pending",
                OrderStatus.PaymentFailed => "Failed",
                OrderStatus.Cancelled when order.PaidAt.HasValue => "Refunded",
                OrderStatus.Refunded => "Refunded",
                _ when order.PaidAt.HasValue => "Paid",
                _ => "Pending"
            };

            rows.Add(new SellerOrderReportRowDto(
                order.Id,
                shipment.Id,
                order.OrderNumber,
                shipment.CreatedAt,
                buyerName,
                shipment.Status.ToString(),
                paymentStatus,
                orderValue,
                commissionAmount,
                netAmount,
                order.Currency));

            totalOrderValue += orderValue;
            totalCommission += commissionAmount;
            totalNetAmount += netAmount;
        }

        var exceedsThreshold = totalCount > ExportThreshold;

        return new SellerOrderReportResultDto(
            rows.AsReadOnly(),
            totalCount,
            pageNumber,
            pageSize,
            totalOrderValue,
            totalCommission,
            totalNetAmount,
            currency,
            exceedsThreshold);
    }

    /// <summary>
    /// Exports seller order/revenue report data to CSV.
    /// Includes only orders that belong to the seller's store.
    /// </summary>
    public async Task<ExportResultDto> HandleAsync(
        ExportSellerOrderReportQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Parse status filter
        ShipmentStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(query.OrderStatus) &&
            Enum.TryParse<ShipmentStatus>(query.OrderStatus, ignoreCase: true, out var parsedStatus))
        {
            statusFilter = parsedStatus;
        }

        // Get all shipments matching the filter criteria
        var shipmentsData = await _orderRepository.GetAllShipmentsForExportAsync(
            query.StoreId,
            statusFilter,
            query.FromDate,
            query.ToDate,
            null, // No buyer search for report
            null, // No tracking filter for report
            cancellationToken);

        if (shipmentsData.Count == 0)
        {
            // Return an empty CSV with headers only
            return GenerateEmptyCsvExport();
        }

        // Get buyer info for the orders
        var buyerIds = shipmentsData.Select(s => s.Order.BuyerId).Distinct().ToList();
        var buyers = new Dictionary<Guid, User?>();
        foreach (var buyerId in buyerIds)
        {
            buyers[buyerId] = await _userRepository.GetByIdAsync(buyerId, cancellationToken);
        }

        // Build export rows
        var exportRows = new List<ReportExportRow>();
        foreach (var data in shipmentsData)
        {
            var buyer = buyers.GetValueOrDefault(data.Order.BuyerId);
            var buyerName = buyer?.FirstName is not null && buyer?.LastName is not null
                ? $"{buyer.FirstName} {buyer.LastName}"
                : data.Order.RecipientName;

            // Calculate commission for this shipment
            var commission = _commissionCalculator.CalculateCommission(
                data.Shipment.StoreId,
                data.Shipment.Subtotal,
                data.Order.Currency);

            var orderValue = data.Shipment.Subtotal + data.Shipment.ShippingCost;
            var commissionAmount = commission.CommissionAmount.Amount;
            var netAmount = commission.SellerPayout.Amount + data.Shipment.ShippingCost;

            // Determine payment status from order
            var paymentStatus = data.Order.Status switch
            {
                OrderStatus.Pending => "Pending",
                OrderStatus.PaymentFailed => "Failed",
                OrderStatus.Cancelled when data.Order.PaidAt.HasValue => "Refunded",
                OrderStatus.Refunded => "Refunded",
                _ when data.Order.PaidAt.HasValue => "Paid",
                _ => "Pending"
            };

            exportRows.Add(new ReportExportRow(
                data.Order.Id,
                data.Shipment.Id,
                data.Order.OrderNumber,
                data.Shipment.CreatedAt,
                buyerName,
                data.Shipment.Status.ToString(),
                paymentStatus,
                orderValue,
                commissionAmount,
                netAmount,
                data.Order.Currency));
        }

        // Generate CSV
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        return GenerateCsvExport(exportRows, timestamp);
    }

    private static ExportResultDto GenerateEmptyCsvExport()
    {
        var sb = new StringBuilder();

        // UTF-8 BOM for Excel compatibility
        sb.Append('\uFEFF');

        // Header row only
        sb.AppendLine("Order ID,Sub-Order ID,Order Number,Order Date,Buyer Name,Order Status,Payment Status,Order Value,Commission,Net Amount,Currency");

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var fileName = $"seller-order-report-{timestamp}.csv";

        return ExportResultDto.Success(bytes, fileName, "text/csv", 0);
    }

    private static ExportResultDto GenerateCsvExport(IReadOnlyCollection<ReportExportRow> rows, string timestamp)
    {
        var sb = new StringBuilder();

        // UTF-8 BOM for Excel compatibility
        sb.Append('\uFEFF');

        // Header row
        sb.AppendLine("Order ID,Sub-Order ID,Order Number,Order Date,Buyer Name,Order Status,Payment Status,Order Value,Commission,Net Amount,Currency");

        // Data rows
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsvValue(row.OrderId.ToString()),
                EscapeCsvValue(row.SubOrderId.ToString()),
                EscapeCsvValue(row.OrderNumber),
                EscapeCsvValue(row.OrderDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
                EscapeCsvValue(row.BuyerName),
                EscapeCsvValue(row.OrderStatus),
                EscapeCsvValue(row.PaymentStatus),
                row.OrderValue.ToString("F2", CultureInfo.InvariantCulture),
                row.Commission.ToString("F2", CultureInfo.InvariantCulture),
                row.NetAmount.ToString("F2", CultureInfo.InvariantCulture),
                EscapeCsvValue(row.Currency)));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"seller-order-report-{timestamp}.csv";

        return ExportResultDto.Success(bytes, fileName, "text/csv", rows.Count);
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

    /// <summary>
    /// Internal record for report export data.
    /// </summary>
    private sealed record ReportExportRow(
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
}
