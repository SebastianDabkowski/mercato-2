using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for order-related queries and operations.
/// Handles buyer order details with sub-order breakdown and seller sub-order views.
/// </summary>
public sealed class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IUserRepository _userRepository;
    private readonly IShippingMethodRepository _shippingMethodRepository;

    public OrderService(
        IOrderRepository orderRepository,
        IStoreRepository storeRepository,
        IUserRepository userRepository,
        IShippingMethodRepository shippingMethodRepository)
    {
        _orderRepository = orderRepository;
        _storeRepository = storeRepository;
        _userRepository = userRepository;
        _shippingMethodRepository = shippingMethodRepository;
    }

    /// <summary>
    /// Gets order details for a buyer with seller sub-order breakdown.
    /// </summary>
    public async Task<BuyerOrderDetailsDto?> HandleAsync(
        GetBuyerOrderDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var order = await _orderRepository.GetByIdAsync(query.OrderId, cancellationToken);
        if (order is null || order.BuyerId != query.BuyerId)
        {
            return null;
        }

        // Get store names for all sellers
        var storeIds = order.Shipments.Select(s => s.StoreId).Distinct().ToList();
        var stores = await _storeRepository.GetByIdsAsync(storeIds, cancellationToken);
        var storeLookup = stores.ToDictionary(s => s.Id);

        // Get shipping methods for estimated delivery
        var shippingMethodIds = order.Items
            .Where(i => i.ShippingMethodId.HasValue)
            .Select(i => i.ShippingMethodId!.Value)
            .Distinct()
            .ToList();
        var shippingMethods = await _shippingMethodRepository.GetByIdsAsync(shippingMethodIds, cancellationToken);
        var shippingMethodLookup = shippingMethods.ToDictionary(m => m.Id);

        var addressSummary = $"{order.DeliveryStreet}, {order.DeliveryCity}, {order.DeliveryPostalCode}, {order.DeliveryCountry}";

        // Build seller sub-order sections
        var sellerSubOrders = new List<SellerSubOrderSectionDto>();
        foreach (var shipment in order.Shipments)
        {
            var store = storeLookup.GetValueOrDefault(shipment.StoreId);
            var storeName = store?.Name ?? "Unknown Seller";

            // Get items for this seller
            var sellerItems = order.Items.Where(i => i.StoreId == shipment.StoreId).ToList();
            var itemDtos = sellerItems.Select(i =>
            {
                string? estimatedDelivery = null;
                if (i.ShippingMethodId.HasValue && shippingMethodLookup.TryGetValue(i.ShippingMethodId.Value, out var method))
                {
                    estimatedDelivery = method.GetDeliveryTimeDisplay();
                }

                return new OrderItemDto(
                    i.ProductId,
                    i.ProductName,
                    i.StoreId,
                    storeName,
                    i.UnitPrice,
                    i.Quantity,
                    i.LineTotal,
                    i.ShippingMethodName,
                    i.ShippingCost,
                    estimatedDelivery);
            }).ToList();

            // Calculate estimated delivery for this sub-order
            string? subOrderEstimatedDelivery = null;
            var itemShippingMethodIds = sellerItems
                .Where(i => i.ShippingMethodId.HasValue)
                .Select(i => i.ShippingMethodId!.Value)
                .Distinct()
                .ToList();

            if (itemShippingMethodIds.Count > 0)
            {
                var relevantMethods = itemShippingMethodIds
                    .Select(id => shippingMethodLookup.GetValueOrDefault(id))
                    .OfType<ShippingMethod>()
                    .ToList();

                if (relevantMethods.Count > 0)
                {
                    var minDays = relevantMethods.Min(m => m.EstimatedDeliveryDaysMin);
                    var maxDays = relevantMethods.Max(m => m.EstimatedDeliveryDaysMax);
                    var minDate = order.CreatedAt.AddDays(minDays).ToString("MMM dd");
                    var maxDate = order.CreatedAt.AddDays(maxDays).ToString("MMM dd, yyyy");
                    subOrderEstimatedDelivery = $"{minDate} - {maxDate}";
                }
            }

            sellerSubOrders.Add(new SellerSubOrderSectionDto(
                shipment.Id,
                shipment.StoreId,
                storeName,
                shipment.Status.ToString(),
                shipment.Subtotal,
                shipment.ShippingCost,
                shipment.Subtotal + shipment.ShippingCost,
                itemDtos.AsReadOnly(),
                shipment.CarrierName,
                shipment.TrackingNumber,
                shipment.TrackingUrl,
                shipment.ShippedAt,
                shipment.DeliveredAt,
                subOrderEstimatedDelivery));
        }

        // Calculate overall estimated delivery range
        string? estimatedDeliveryRange = null;
        if (shippingMethods.Count > 0)
        {
            var minDays = shippingMethods.Min(m => m.EstimatedDeliveryDaysMin);
            var maxDays = shippingMethods.Max(m => m.EstimatedDeliveryDaysMax);
            var minDate = order.CreatedAt.AddDays(minDays).ToString("MMM dd");
            var maxDate = order.CreatedAt.AddDays(maxDays).ToString("MMM dd, yyyy");
            estimatedDeliveryRange = $"{minDate} - {maxDate}";
        }

        return new BuyerOrderDetailsDto(
            order.Id,
            order.OrderNumber,
            order.Status.ToString(),
            order.RecipientName,
            addressSummary,
            order.PaymentMethodName,
            order.ItemSubtotal,
            order.TotalShipping,
            order.TotalAmount,
            order.Currency,
            order.CreatedAt,
            estimatedDeliveryRange,
            sellerSubOrders.AsReadOnly());
    }

    /// <summary>
    /// Gets seller's sub-orders with pagination.
    /// </summary>
    public async Task<IReadOnlyList<SellerSubOrderSummaryDto>> HandleAsync(
        GetSellerSubOrdersQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var shipments = await _orderRepository.GetShipmentsByStoreIdAsync(
            query.StoreId,
            query.Skip,
            query.Take,
            cancellationToken);

        if (shipments.Count == 0)
        {
            return Array.Empty<SellerSubOrderSummaryDto>();
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

        var result = new List<SellerSubOrderSummaryDto>();
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

            // Count items for this sub-order
            var itemCount = order.Items.Count(i => i.StoreId == shipment.StoreId);

            result.Add(new SellerSubOrderSummaryDto(
                shipment.Id,
                order.Id,
                order.OrderNumber,
                shipment.Status.ToString(),
                itemCount,
                shipment.Subtotal + shipment.ShippingCost,
                order.Currency,
                buyerName,
                shipment.CreatedAt));
        }

        return result.AsReadOnly();
    }

    /// <summary>
    /// Gets details of a specific seller sub-order.
    /// </summary>
    public async Task<SellerSubOrderDto?> HandleAsync(
        GetSellerSubOrderDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (shipment, order, items) = await _orderRepository.GetShipmentWithOrderAsync(
            query.SubOrderId,
            cancellationToken);

        if (shipment is null || order is null)
        {
            return null;
        }

        // Verify the shipment belongs to the requested store
        if (shipment.StoreId != query.StoreId)
        {
            return null;
        }

        var buyer = await _userRepository.GetByIdAsync(order.BuyerId, cancellationToken);
        var buyerName = buyer?.FirstName is not null && buyer?.LastName is not null
            ? $"{buyer.FirstName} {buyer.LastName}"
            : order.RecipientName;

        // Get buyer contact info - only expose minimal data per GDPR rules
        var buyerEmail = buyer?.Email?.Value;
        var buyerPhone = order.DeliveryPhoneNumber ?? buyer?.PhoneNumber;

        // Build full delivery address
        var addressParts = new List<string> { order.DeliveryStreet };
        if (!string.IsNullOrWhiteSpace(order.DeliveryStreet2))
        {
            addressParts.Add(order.DeliveryStreet2);
        }
        addressParts.Add(order.DeliveryCity);
        if (!string.IsNullOrWhiteSpace(order.DeliveryState))
        {
            addressParts.Add(order.DeliveryState);
        }
        addressParts.Add(order.DeliveryPostalCode);
        addressParts.Add(order.DeliveryCountry);
        var deliveryAddress = string.Join(", ", addressParts);

        // Get primary shipping method name for this seller's items
        var shippingMethodName = items.FirstOrDefault(i => !string.IsNullOrEmpty(i.ShippingMethodName))?.ShippingMethodName;

        var itemDtos = items.Select(i => new SellerSubOrderItemDto(
            i.Id,
            i.ProductId,
            i.ProductName,
            i.UnitPrice,
            i.Quantity,
            i.LineTotal,
            i.ShippingMethodName)).ToList();

        // Determine abstract payment status (not exposing sensitive financial data)
        var paymentStatus = order.Status switch
        {
            OrderStatus.Pending => "Pending",
            OrderStatus.PaymentFailed => "Failed",
            OrderStatus.Cancelled => "Cancelled",
            OrderStatus.Refunded => "Refunded",
            _ => "Paid"
        };

        return new SellerSubOrderDto(
            shipment.Id,
            order.Id,
            order.OrderNumber,
            shipment.Status.ToString(),
            paymentStatus,
            shipment.Subtotal,
            shipment.ShippingCost,
            shipment.Subtotal + shipment.ShippingCost,
            order.Currency,
            buyerName,
            buyerEmail,
            buyerPhone,
            deliveryAddress,
            order.DeliveryInstructions,
            shippingMethodName,
            itemDtos.AsReadOnly(),
            shipment.CreatedAt,
            order.PaidAt,
            shipment.Status >= ShipmentStatus.Processing ? shipment.UpdatedAt : null,
            shipment.ShippedAt,
            shipment.DeliveredAt,
            shipment.CancelledAt,
            shipment.RefundedAt,
            shipment.CarrierName,
            shipment.TrackingNumber,
            shipment.TrackingUrl);
    }

    /// <summary>
    /// Gets seller's sub-orders with filtering and pagination.
    /// </summary>
    public async Task<PagedResultDto<SellerSubOrderSummaryDto>> HandleAsync(
        GetFilteredSellerSubOrdersQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Validate and normalize pagination
        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var skip = (pageNumber - 1) * pageSize;

        // Parse status filter if provided
        ShipmentStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(query.Status) && 
            Enum.TryParse<ShipmentStatus>(query.Status, ignoreCase: true, out var parsedStatus))
        {
            statusFilter = parsedStatus;
        }

        // Get filtered shipments from repository
        var (shipments, totalCount) = await _orderRepository.GetFilteredShipmentsByStoreIdAsync(
            query.StoreId,
            statusFilter,
            query.FromDate,
            query.ToDate,
            query.BuyerSearch,
            skip,
            pageSize,
            cancellationToken);

        if (shipments.Count == 0)
        {
            return PagedResultDto<SellerSubOrderSummaryDto>.Create(
                Array.Empty<SellerSubOrderSummaryDto>(),
                pageNumber,
                pageSize,
                totalCount);
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

        var result = new List<SellerSubOrderSummaryDto>();
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

            // Count items for this sub-order
            var itemCount = order.Items.Count(i => i.StoreId == shipment.StoreId);

            result.Add(new SellerSubOrderSummaryDto(
                shipment.Id,
                order.Id,
                order.OrderNumber,
                shipment.Status.ToString(),
                itemCount,
                shipment.Subtotal + shipment.ShippingCost,
                order.Currency,
                buyerName,
                shipment.CreatedAt));
        }

        return PagedResultDto<SellerSubOrderSummaryDto>.Create(
            result.AsReadOnly(),
            pageNumber,
            pageSize,
            totalCount);
    }
}
