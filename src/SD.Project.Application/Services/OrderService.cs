using SD.Project.Application.DTOs;
using SD.Project.Application.Commands;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Domain.Services;

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
    private readonly INotificationService _notificationService;
    private readonly EscrowService _escrowService;
    private readonly IShipmentStatusHistoryRepository _statusHistoryRepository;

    public OrderService(
        IOrderRepository orderRepository,
        IStoreRepository storeRepository,
        IUserRepository userRepository,
        IShippingMethodRepository shippingMethodRepository,
        INotificationService notificationService,
        EscrowService escrowService,
        IShipmentStatusHistoryRepository statusHistoryRepository)
    {
        ArgumentNullException.ThrowIfNull(orderRepository);
        ArgumentNullException.ThrowIfNull(storeRepository);
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(shippingMethodRepository);
        ArgumentNullException.ThrowIfNull(notificationService);
        ArgumentNullException.ThrowIfNull(escrowService);
        ArgumentNullException.ThrowIfNull(statusHistoryRepository);

        _orderRepository = orderRepository;
        _storeRepository = storeRepository;
        _userRepository = userRepository;
        _shippingMethodRepository = shippingMethodRepository;
        _notificationService = notificationService;
        _escrowService = escrowService;
        _statusHistoryRepository = statusHistoryRepository;
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
                // Prefer stored delivery time (historical value at order creation)
                // Fall back to current shipping method value for backwards compatibility
                string? estimatedDelivery = i.GetEstimatedDeliveryDisplay();
                if (estimatedDelivery is null 
                    && i.ShippingMethodId.HasValue 
                    && shippingMethodLookup.TryGetValue(i.ShippingMethodId.Value, out var method))
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
                    estimatedDelivery,
                    i.Status.ToString(),
                    i.CarrierName,
                    i.TrackingNumber,
                    i.TrackingUrl,
                    i.ShippedAt,
                    i.DeliveredAt,
                    i.CancelledAt,
                    i.RefundedAt,
                    i.RefundedAmount);
            }).ToList();

            // Calculate estimated delivery for this sub-order
            // Prefer stored delivery time (historical value at order creation)
            string? subOrderEstimatedDelivery = null;
            var itemsWithDeliveryTime = sellerItems
                .Where(i => i.EstimatedDeliveryDaysMin.HasValue && i.EstimatedDeliveryDaysMax.HasValue)
                .ToList();

            if (itemsWithDeliveryTime.Count > 0)
            {
                var minDays = itemsWithDeliveryTime.Min(i => i.EstimatedDeliveryDaysMin!.Value);
                var maxDays = itemsWithDeliveryTime.Max(i => i.EstimatedDeliveryDaysMax!.Value);
                var minDate = order.CreatedAt.AddDays(minDays).ToString("MMM dd");
                var maxDate = order.CreatedAt.AddDays(maxDays).ToString("MMM dd, yyyy");
                subOrderEstimatedDelivery = $"{minDate} - {maxDate}";
            }
            else
            {
                // Backwards compatibility: fall back to current shipping method values
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
                subOrderEstimatedDelivery,
                shipment.CancelledAt,
                shipment.RefundedAt,
                shipment.RefundedAmount));
        }

        // Calculate overall estimated delivery range from stored order item values
        // Fall back to current shipping method values for backwards compatibility
        string? estimatedDeliveryRange = null;
        var allItemsWithDeliveryTime = order.Items
            .Where(i => i.EstimatedDeliveryDaysMin.HasValue && i.EstimatedDeliveryDaysMax.HasValue)
            .ToList();
        
        if (allItemsWithDeliveryTime.Count > 0)
        {
            var minDays = allItemsWithDeliveryTime.Min(i => i.EstimatedDeliveryDaysMin!.Value);
            var maxDays = allItemsWithDeliveryTime.Max(i => i.EstimatedDeliveryDaysMax!.Value);
            var minDate = order.CreatedAt.AddDays(minDays).ToString("MMM dd");
            var maxDate = order.CreatedAt.AddDays(maxDays).ToString("MMM dd, yyyy");
            estimatedDeliveryRange = $"{minDate} - {maxDate}";
        }
        else if (shippingMethods.Count > 0)
        {
            // Backwards compatibility: fall back to current shipping method values
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
            order.PaymentStatus.ToString(),
            PaymentStatusMapper.GetBuyerFriendlyMessage(order.PaymentStatus),
            order.RecipientName,
            addressSummary,
            order.PaymentMethodName,
            order.ItemSubtotal,
            order.TotalShipping,
            order.TotalAmount,
            order.Currency,
            order.CreatedAt,
            estimatedDeliveryRange,
            sellerSubOrders.AsReadOnly(),
            order.CancelledAt,
            order.RefundedAt,
            order.RefundedAmount);
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
            i.ShippingMethodName,
            i.Status.ToString(),
            i.CarrierName,
            i.TrackingNumber,
            i.TrackingUrl,
            i.ShippedAt,
            i.DeliveredAt,
            i.CancelledAt,
            i.RefundedAt,
            i.RefundedAmount)).ToList();

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
            query.WithoutTracking,
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

    /// <summary>
    /// Updates the status of a shipment.
    /// </summary>
    public async Task<UpdateShipmentStatusResultDto> HandleAsync(
        UpdateShipmentStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Get the shipment with order
        var (shipment, order, _) = await _orderRepository.GetShipmentWithOrderAsync(
            command.ShipmentId,
            cancellationToken);

        if (shipment is null || order is null)
        {
            return new UpdateShipmentStatusResultDto(false, "Shipment not found.");
        }

        // Verify the shipment belongs to the requested store
        if (shipment.StoreId != command.StoreId)
        {
            return new UpdateShipmentStatusResultDto(false, "Shipment does not belong to this store.");
        }

        // Parse the target status
        if (!Enum.TryParse<ShipmentStatus>(command.NewStatus, ignoreCase: true, out var targetStatus))
        {
            return new UpdateShipmentStatusResultDto(false, $"Invalid status: {command.NewStatus}.");
        }

        // Validate the transition
        if (!shipment.CanTransitionTo(targetStatus))
        {
            return new UpdateShipmentStatusResultDto(
                false,
                $"Cannot transition from {shipment.Status} to {targetStatus}.");
        }

        var previousStatus = shipment.Status;

        // Apply the status change
        try
        {
            switch (targetStatus)
            {
                case ShipmentStatus.Processing:
                    shipment.StartProcessing();
                    break;
                case ShipmentStatus.Shipped:
                    shipment.Ship(command.CarrierName, command.TrackingNumber, command.TrackingUrl);
                    break;
                case ShipmentStatus.Delivered:
                    shipment.MarkDelivered();
                    // Mark escrow allocation as eligible for payout when delivered
                    await _escrowService.HandleAsync(
                        new MarkEscrowEligibleCommand(shipment.Id),
                        cancellationToken);
                    break;
                case ShipmentStatus.Cancelled:
                    shipment.Cancel();
                    // Refund escrow allocation when shipment is cancelled
                    await _escrowService.HandleAsync(
                        new RefundShipmentEscrowCommand(shipment.Id),
                        cancellationToken);
                    break;
                case ShipmentStatus.Refunded:
                    shipment.Refund();
                    break;
                default:
                    return new UpdateShipmentStatusResultDto(
                        false,
                        $"Status transition to {targetStatus} is not supported.");
            }
        }
        catch (InvalidOperationException ex)
        {
            return new UpdateShipmentStatusResultDto(false, ex.Message);
        }

        // Record status change history
        var statusHistory = new ShipmentStatusHistory(
            shipment.Id,
            order.Id,
            previousStatus,
            shipment.Status,
            command.UpdatedByUserId,
            StatusChangeActorType.Seller,
            command.CarrierName,
            command.TrackingNumber,
            command.TrackingUrl);
        await _statusHistoryRepository.AddAsync(statusHistory, cancellationToken);

        // Save changes
        await _orderRepository.SaveChangesAsync(cancellationToken);
        await _statusHistoryRepository.SaveChangesAsync(cancellationToken);

        // Send notification to buyer
        var buyer = await _userRepository.GetByIdAsync(order.BuyerId, cancellationToken);
        if (buyer?.Email is not null)
        {
            await _notificationService.SendShipmentStatusChangedAsync(
                shipment.Id,
                order.Id,
                buyer.Email.Value,
                order.OrderNumber,
                previousStatus.ToString(),
                shipment.Status.ToString(),
                shipment.TrackingNumber,
                shipment.CarrierName,
                cancellationToken);
        }

        return new UpdateShipmentStatusResultDto(
            true,
            null,
            previousStatus.ToString(),
            shipment.Status.ToString());
    }

    /// <summary>
    /// Updates tracking information for a shipped order.
    /// </summary>
    public async Task<UpdateShipmentStatusResultDto> HandleAsync(
        UpdateTrackingInfoCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Get the shipment with order
        var (shipment, order, _) = await _orderRepository.GetShipmentWithOrderAsync(
            command.ShipmentId,
            cancellationToken);

        if (shipment is null || order is null)
        {
            return new UpdateShipmentStatusResultDto(false, "Shipment not found.");
        }

        // Verify the shipment belongs to the requested store
        if (shipment.StoreId != command.StoreId)
        {
            return new UpdateShipmentStatusResultDto(false, "Shipment does not belong to this store.");
        }

        // Update tracking info
        try
        {
            shipment.UpdateTrackingInfo(command.CarrierName, command.TrackingNumber, command.TrackingUrl);
        }
        catch (InvalidOperationException ex)
        {
            return new UpdateShipmentStatusResultDto(false, ex.Message);
        }

        // Save changes
        await _orderRepository.SaveChangesAsync(cancellationToken);

        // Send notification to buyer about updated tracking info
        var buyer = await _userRepository.GetByIdAsync(order.BuyerId, cancellationToken);
        if (buyer?.Email is not null)
        {
            await _notificationService.SendTrackingInfoUpdatedAsync(
                shipment.Id,
                order.Id,
                buyer.Email.Value,
                order.OrderNumber,
                shipment.TrackingNumber,
                shipment.CarrierName,
                shipment.TrackingUrl,
                cancellationToken);
        }

        return new UpdateShipmentStatusResultDto(
            true,
            null,
            shipment.Status.ToString(),
            shipment.Status.ToString());
    }

    /// <summary>
    /// Cancels a shipment (before it is shipped).
    /// </summary>
    public async Task<UpdateShipmentStatusResultDto> HandleAsync(
        CancelShipmentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Get the shipment with order
        var (shipment, order, _) = await _orderRepository.GetShipmentWithOrderAsync(
            command.ShipmentId,
            cancellationToken);

        if (shipment is null || order is null)
        {
            return new UpdateShipmentStatusResultDto(false, "Shipment not found.");
        }

        // Verify the shipment belongs to the requested store
        if (shipment.StoreId != command.StoreId)
        {
            return new UpdateShipmentStatusResultDto(false, "Shipment does not belong to this store.");
        }

        var previousStatus = shipment.Status;

        // Cancel the shipment
        try
        {
            shipment.Cancel();
        }
        catch (InvalidOperationException ex)
        {
            return new UpdateShipmentStatusResultDto(false, ex.Message);
        }

        // Record status change history
        var statusHistory = new ShipmentStatusHistory(
            shipment.Id,
            order.Id,
            previousStatus,
            ShipmentStatus.Cancelled,
            command.CancelledByUserId,
            StatusChangeActorType.Seller);
        await _statusHistoryRepository.AddAsync(statusHistory, cancellationToken);

        // Refund escrow allocation when shipment is cancelled
        await _escrowService.HandleAsync(
            new RefundShipmentEscrowCommand(command.ShipmentId),
            cancellationToken);

        // Save changes
        await _orderRepository.SaveChangesAsync(cancellationToken);
        await _statusHistoryRepository.SaveChangesAsync(cancellationToken);

        // Send notification to buyer
        var buyer = await _userRepository.GetByIdAsync(order.BuyerId, cancellationToken);
        if (buyer?.Email is not null)
        {
            await _notificationService.SendShipmentStatusChangedAsync(
                shipment.Id,
                order.Id,
                buyer.Email.Value,
                order.OrderNumber,
                previousStatus.ToString(),
                ShipmentStatus.Cancelled.ToString(),
                null,
                null,
                cancellationToken);
        }

        return new UpdateShipmentStatusResultDto(
            true,
            null,
            previousStatus.ToString(),
            ShipmentStatus.Cancelled.ToString());
    }

    /// <summary>
    /// Gets available status transitions for a shipment.
    /// </summary>
    public async Task<ShipmentStatusTransitionsDto?> GetShipmentStatusTransitionsAsync(
        Guid storeId,
        Guid shipmentId,
        CancellationToken cancellationToken = default)
    {
        var (shipment, _, _) = await _orderRepository.GetShipmentWithOrderAsync(
            shipmentId,
            cancellationToken);

        if (shipment is null || shipment.StoreId != storeId)
        {
            return null;
        }

        var availableTransitions = new List<string>();
        foreach (ShipmentStatus status in Enum.GetValues<ShipmentStatus>())
        {
            if (shipment.CanTransitionTo(status))
            {
                availableTransitions.Add(status.ToString());
            }
        }

        // Can update tracking when shipped
        var canUpdateTracking = shipment.Status == ShipmentStatus.Shipped;

        // Can cancel before shipped
        var canCancel = shipment.CanTransitionTo(ShipmentStatus.Cancelled);

        return new ShipmentStatusTransitionsDto(
            shipment.Status.ToString(),
            availableTransitions.AsReadOnly(),
            canUpdateTracking,
            canCancel);
    }

    /// <summary>
    /// Gets admin order details with full shipping status history.
    /// </summary>
    public async Task<AdminOrderDetailsDto?> HandleAsync(
        GetAdminOrderDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var order = await _orderRepository.GetByIdAsync(query.OrderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        // Get buyer information
        var buyer = await _userRepository.GetByIdAsync(order.BuyerId, cancellationToken);
        var buyerName = buyer?.FirstName is not null && buyer?.LastName is not null
            ? $"{buyer.FirstName} {buyer.LastName}"
            : order.RecipientName;
        var buyerEmail = buyer?.Email?.Value;

        // Get store names for all sellers
        var storeIds = order.Shipments.Select(s => s.StoreId).Distinct().ToList();
        var stores = await _storeRepository.GetByIdsAsync(storeIds, cancellationToken);
        var storeLookup = stores.ToDictionary(s => s.Id);

        // Get all status history for this order
        var orderStatusHistory = await _statusHistoryRepository.GetByOrderIdAsync(order.Id, cancellationToken);
        var historyLookup = orderStatusHistory.GroupBy(h => h.ShipmentId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Get user names for status history entries using bulk loading
        var changedByUserIds = orderStatusHistory
            .Where(h => h.ChangedByUserId.HasValue)
            .Select(h => h.ChangedByUserId!.Value)
            .Distinct()
            .ToList();
        var users = await _userRepository.GetByIdsAsync(changedByUserIds, cancellationToken);
        var userLookup = users.ToDictionary(
            u => u.Id,
            u => u.FirstName is not null && u.LastName is not null
                ? $"{u.FirstName} {u.LastName}"
                : u.Email?.Value ?? u.Id.ToString());

        var addressSummary = $"{order.DeliveryStreet}, {order.DeliveryCity}, {order.DeliveryPostalCode}, {order.DeliveryCountry}";

        // Build shipments with status history
        var shipmentDtos = new List<AdminShipmentDto>();
        foreach (var shipment in order.Shipments)
        {
            var store = storeLookup.GetValueOrDefault(shipment.StoreId);
            var storeName = store?.Name ?? "Unknown Seller";

            // Get items for this shipment
            var sellerItems = order.Items.Where(i => i.StoreId == shipment.StoreId).ToList();
            var itemDtos = sellerItems.Select(i => new AdminOrderItemDto(
                i.Id,
                i.ProductId,
                i.ProductName,
                i.UnitPrice,
                i.Quantity,
                i.LineTotal,
                i.ShippingMethodName,
                i.Status.ToString())).ToList();

            // Get status history for this shipment
            var shipmentHistory = historyLookup.GetValueOrDefault(shipment.Id) ?? new List<ShipmentStatusHistory>();
            var historyDtos = shipmentHistory.Select(h => new ShipmentStatusHistoryDto(
                h.Id,
                h.ShipmentId,
                h.OrderId,
                h.PreviousStatus.ToString(),
                h.NewStatus.ToString(),
                h.ChangedAt,
                h.ChangedByUserId,
                h.ChangedByUserId.HasValue && userLookup.TryGetValue(h.ChangedByUserId.Value, out var userName)
                    ? userName
                    : null,
                h.ActorType.ToString(),
                h.CarrierName,
                h.TrackingNumber,
                h.TrackingUrl,
                h.Notes)).ToList();

            shipmentDtos.Add(new AdminShipmentDto(
                shipment.Id,
                shipment.StoreId,
                storeName,
                shipment.Status.ToString(),
                shipment.Subtotal,
                shipment.ShippingCost,
                shipment.Subtotal + shipment.ShippingCost,
                shipment.CarrierName,
                shipment.TrackingNumber,
                shipment.TrackingUrl,
                shipment.CreatedAt,
                shipment.ShippedAt,
                shipment.DeliveredAt,
                shipment.CancelledAt,
                shipment.RefundedAt,
                shipment.RefundedAmount,
                itemDtos.AsReadOnly(),
                historyDtos.AsReadOnly()));
        }

        return new AdminOrderDetailsDto(
            order.Id,
            order.OrderNumber,
            order.Status.ToString(),
            order.PaymentStatus.ToString(),
            order.BuyerId,
            buyerName,
            buyerEmail,
            order.RecipientName,
            addressSummary,
            order.PaymentMethodName,
            order.PaymentTransactionId,
            order.ItemSubtotal,
            order.TotalShipping,
            order.TotalAmount,
            order.Currency,
            order.CreatedAt,
            order.PaidAt,
            order.CancelledAt,
            order.RefundedAt,
            order.RefundedAmount,
            shipmentDtos.AsReadOnly());
    }

    /// <summary>
    /// Gets shipping status history for a specific shipment.
    /// </summary>
    public async Task<IReadOnlyList<ShipmentStatusHistoryDto>> HandleAsync(
        GetShipmentStatusHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var history = await _statusHistoryRepository.GetByShipmentIdAsync(query.ShipmentId, cancellationToken);

        // Get user names for status history entries using bulk loading
        var changedByUserIds = history
            .Where(h => h.ChangedByUserId.HasValue)
            .Select(h => h.ChangedByUserId!.Value)
            .Distinct()
            .ToList();
        var users = await _userRepository.GetByIdsAsync(changedByUserIds, cancellationToken);
        var userLookup = users.ToDictionary(
            u => u.Id,
            u => u.FirstName is not null && u.LastName is not null
                ? $"{u.FirstName} {u.LastName}"
                : u.Email?.Value ?? u.Id.ToString());

        return history.Select(h => new ShipmentStatusHistoryDto(
            h.Id,
            h.ShipmentId,
            h.OrderId,
            h.PreviousStatus.ToString(),
            h.NewStatus.ToString(),
            h.ChangedAt,
            h.ChangedByUserId,
            h.ChangedByUserId.HasValue && userLookup.TryGetValue(h.ChangedByUserId.Value, out var userName)
                ? userName
                : null,
            h.ActorType.ToString(),
            h.CarrierName,
            h.TrackingNumber,
            h.TrackingUrl,
            h.Notes)).ToList().AsReadOnly();
    }
}
