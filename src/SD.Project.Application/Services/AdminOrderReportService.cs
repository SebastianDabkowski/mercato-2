using System.Globalization;
using System.Text;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Domain.Services;
using SD.Project.Domain.ValueObjects;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for admin order and revenue reports.
/// </summary>
public sealed class AdminOrderReportService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IUserRepository _userRepository;
    private readonly CommissionCalculator _commissionCalculator;

    /// <summary>
    /// Maximum number of rows that can be exported without warning.
    /// Above this threshold, the system will warn about large exports.
    /// </summary>
    public const int ExportThreshold = 10000;

    public AdminOrderReportService(
        IOrderRepository orderRepository,
        IStoreRepository storeRepository,
        IUserRepository userRepository,
        CommissionCalculator commissionCalculator)
    {
        ArgumentNullException.ThrowIfNull(orderRepository);
        ArgumentNullException.ThrowIfNull(storeRepository);
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(commissionCalculator);

        _orderRepository = orderRepository;
        _storeRepository = storeRepository;
        _userRepository = userRepository;
        _commissionCalculator = commissionCalculator;
    }

    /// <summary>
    /// Gets filtered admin order/revenue report data with pagination.
    /// </summary>
    public async Task<AdminOrderReportResultDto> HandleAsync(
        GetAdminOrderReportQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Parse status filters
        OrderStatus? orderStatus = null;
        if (!string.IsNullOrWhiteSpace(query.OrderStatus) &&
            Enum.TryParse<OrderStatus>(query.OrderStatus, ignoreCase: true, out var parsedOrderStatus))
        {
            orderStatus = parsedOrderStatus;
        }

        PaymentStatus? paymentStatus = null;
        if (!string.IsNullOrWhiteSpace(query.PaymentStatus) &&
            Enum.TryParse<PaymentStatus>(query.PaymentStatus, ignoreCase: true, out var parsedPaymentStatus))
        {
            paymentStatus = parsedPaymentStatus;
        }

        // Validate and normalize pagination
        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var skip = (pageNumber - 1) * pageSize;

        // Get filtered orders from repository
        var (orders, totalCount) = await _orderRepository.GetFilteredOrdersAsync(
            orderStatus,
            paymentStatus,
            query.FromDate,
            query.ToDate,
            query.SellerId,
            skip,
            pageSize,
            cancellationToken);

        if (orders.Count == 0)
        {
            return new AdminOrderReportResultDto(
                Array.Empty<AdminOrderReportRowDto>(),
                0, pageNumber, pageSize,
                0m, 0m, 0m, "PLN",
                false);
        }

        // Get buyer information
        var buyerIds = orders.Select(o => o.BuyerId).Distinct().ToList();
        var buyers = await _userRepository.GetByIdsAsync(buyerIds, cancellationToken);
        var buyerLookup = buyers.ToDictionary(u => u.Id);

        // Get store information for all shipments
        var storeIds = orders.SelectMany(o => o.Shipments.Select(s => s.StoreId)).Distinct().ToList();
        var stores = await _storeRepository.GetByIdsAsync(storeIds, cancellationToken);
        var storeLookup = stores.ToDictionary(s => s.Id);

        // Build report rows - one row per order-shipment combination
        var rows = new List<AdminOrderReportRowDto>();
        decimal totalOrderValue = 0m;
        decimal totalCommission = 0m;
        decimal totalPayoutAmount = 0m;
        string currency = "PLN";

        foreach (var order in orders)
        {
            var buyer = buyerLookup.GetValueOrDefault(order.BuyerId);
            var buyerName = buyer?.FirstName is not null && buyer?.LastName is not null
                ? $"{buyer.FirstName} {buyer.LastName}"
                : order.RecipientName;

            currency = order.Currency;

            // Create a row for each seller shipment in the order
            foreach (var shipment in order.Shipments)
            {
                var store = storeLookup.GetValueOrDefault(shipment.StoreId);
                var sellerName = store?.Name ?? "Unknown Seller";

                // Calculate commission for this shipment
                var commission = _commissionCalculator.CalculateCommission(
                    shipment.StoreId,
                    shipment.Subtotal,
                    order.Currency);

                var orderValue = shipment.Subtotal + shipment.ShippingCost;
                var commissionAmount = commission.CommissionAmount.Amount;
                var payoutAmount = commission.SellerPayout.Amount + shipment.ShippingCost;

                rows.Add(new AdminOrderReportRowDto(
                    order.Id,
                    order.OrderNumber,
                    order.CreatedAt,
                    order.BuyerId,
                    buyerName,
                    shipment.StoreId,
                    sellerName,
                    order.Status.ToString(),
                    order.PaymentStatus.ToString(),
                    orderValue,
                    commissionAmount,
                    payoutAmount,
                    order.Currency));

                totalOrderValue += orderValue;
                totalCommission += commissionAmount;
                totalPayoutAmount += payoutAmount;
            }
        }

        var exceedsThreshold = totalCount > ExportThreshold;

        return new AdminOrderReportResultDto(
            rows.AsReadOnly(),
            totalCount,
            pageNumber,
            pageSize,
            totalOrderValue,
            totalCommission,
            totalPayoutAmount,
            currency,
            exceedsThreshold);
    }

    /// <summary>
    /// Exports admin order/revenue report data to CSV.
    /// </summary>
    public async Task<ExportResultDto> HandleAsync(
        ExportAdminOrderReportQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Parse status filters
        OrderStatus? orderStatus = null;
        if (!string.IsNullOrWhiteSpace(query.OrderStatus) &&
            Enum.TryParse<OrderStatus>(query.OrderStatus, ignoreCase: true, out var parsedOrderStatus))
        {
            orderStatus = parsedOrderStatus;
        }

        PaymentStatus? paymentStatus = null;
        if (!string.IsNullOrWhiteSpace(query.PaymentStatus) &&
            Enum.TryParse<PaymentStatus>(query.PaymentStatus, ignoreCase: true, out var parsedPaymentStatus))
        {
            paymentStatus = parsedPaymentStatus;
        }

        // Get all orders matching the filter criteria
        var orders = await _orderRepository.GetAllOrdersForReportExportAsync(
            orderStatus,
            paymentStatus,
            query.FromDate,
            query.ToDate,
            query.SellerId,
            cancellationToken);

        if (orders.Count == 0)
        {
            return ExportResultDto.Failed("No orders match the current filters.");
        }

        // Get buyer information
        var buyerIds = orders.Select(o => o.BuyerId).Distinct().ToList();
        var buyers = await _userRepository.GetByIdsAsync(buyerIds, cancellationToken);
        var buyerLookup = buyers.ToDictionary(u => u.Id);

        // Get store information for all shipments
        var storeIds = orders.SelectMany(o => o.Shipments.Select(s => s.StoreId)).Distinct().ToList();
        var stores = await _storeRepository.GetByIdsAsync(storeIds, cancellationToken);
        var storeLookup = stores.ToDictionary(s => s.Id);

        // Build export rows
        var exportRows = new List<ReportExportRow>();
        foreach (var order in orders)
        {
            var buyer = buyerLookup.GetValueOrDefault(order.BuyerId);
            var buyerName = buyer?.FirstName is not null && buyer?.LastName is not null
                ? $"{buyer.FirstName} {buyer.LastName}"
                : order.RecipientName;

            foreach (var shipment in order.Shipments)
            {
                var store = storeLookup.GetValueOrDefault(shipment.StoreId);
                var sellerName = store?.Name ?? "Unknown Seller";

                // Calculate commission for this shipment
                var commission = _commissionCalculator.CalculateCommission(
                    shipment.StoreId,
                    shipment.Subtotal,
                    order.Currency);

                var orderValue = shipment.Subtotal + shipment.ShippingCost;
                var commissionAmount = commission.CommissionAmount.Amount;
                var payoutAmount = commission.SellerPayout.Amount + shipment.ShippingCost;

                exportRows.Add(new ReportExportRow(
                    order.Id,
                    order.OrderNumber,
                    order.CreatedAt,
                    order.BuyerId,
                    buyerName,
                    shipment.StoreId,
                    sellerName,
                    order.Status.ToString(),
                    order.PaymentStatus.ToString(),
                    orderValue,
                    commissionAmount,
                    payoutAmount,
                    order.Currency));
            }
        }

        // Generate CSV
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        return GenerateCsvExport(exportRows, timestamp);
    }

    private static ExportResultDto GenerateCsvExport(IReadOnlyCollection<ReportExportRow> rows, string timestamp)
    {
        var sb = new StringBuilder();

        // UTF-8 BOM for Excel compatibility
        sb.Append('\uFEFF');

        // Header row
        sb.AppendLine("Order ID,Order Number,Order Date,Buyer ID,Buyer Name,Seller ID,Seller Name,Order Status,Payment Status,Order Value,Commission,Payout Amount,Currency");

        // Data rows
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsvValue(row.OrderId.ToString()),
                EscapeCsvValue(row.OrderNumber),
                EscapeCsvValue(row.OrderDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
                EscapeCsvValue(row.BuyerId.ToString()),
                EscapeCsvValue(row.BuyerName),
                EscapeCsvValue(row.SellerId.ToString()),
                EscapeCsvValue(row.SellerName),
                EscapeCsvValue(row.OrderStatus),
                EscapeCsvValue(row.PaymentStatus),
                row.OrderValue.ToString("F2", CultureInfo.InvariantCulture),
                row.Commission.ToString("F2", CultureInfo.InvariantCulture),
                row.PayoutAmount.ToString("F2", CultureInfo.InvariantCulture),
                EscapeCsvValue(row.Currency)));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"order-revenue-report-{timestamp}.csv";

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
        string OrderNumber,
        DateTime OrderDate,
        Guid BuyerId,
        string BuyerName,
        Guid SellerId,
        string SellerName,
        string OrderStatus,
        string PaymentStatus,
        decimal OrderValue,
        decimal Commission,
        decimal PayoutAmount,
        string Currency);
}
