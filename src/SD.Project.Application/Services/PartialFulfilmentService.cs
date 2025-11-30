using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for item-level partial fulfilment operations.
/// Enables Phase 2 functionality: partial fulfilment of sub-orders.
/// </summary>
public sealed class PartialFulfilmentService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;

    public PartialFulfilmentService(
        IOrderRepository orderRepository,
        IUserRepository userRepository,
        INotificationService notificationService)
    {
        _orderRepository = orderRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Gets item-level status details for a sub-order.
    /// </summary>
    public async Task<IReadOnlyList<OrderItemStatusDto>?> HandleAsync(
        GetSubOrderItemStatusQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (shipment, order, items) = await _orderRepository.GetShipmentWithOrderAsync(
            query.ShipmentId,
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

        return items.Select(item => new OrderItemStatusDto(
            item.Id,
            item.ProductId,
            item.ProductName,
            item.UnitPrice,
            item.Quantity,
            item.LineTotal,
            item.ShippingCost,
            item.Status.ToString(),
            item.CarrierName,
            item.TrackingNumber,
            item.TrackingUrl,
            item.ShippedAt,
            item.DeliveredAt,
            item.CancelledAt,
            item.RefundedAt,
            item.RefundedAmount)).ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets available status transitions for a specific item.
    /// </summary>
    public async Task<ItemStatusTransitionsDto?> HandleAsync(
        GetItemStatusTransitionsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (shipment, _, items) = await _orderRepository.GetShipmentWithOrderAsync(
            query.ShipmentId,
            cancellationToken);

        if (shipment is null || shipment.StoreId != query.StoreId)
        {
            return null;
        }

        var item = items.FirstOrDefault(i => i.Id == query.ItemId);
        if (item is null)
        {
            return null;
        }

        var availableTransitions = new List<string>();
        foreach (OrderItemStatus status in Enum.GetValues<OrderItemStatus>())
        {
            if (item.CanTransitionTo(status))
            {
                availableTransitions.Add(status.ToString());
            }
        }

        return new ItemStatusTransitionsDto(
            item.Status.ToString(),
            availableTransitions.AsReadOnly(),
            item.Status == OrderItemStatus.Shipped,
            item.CanTransitionTo(OrderItemStatus.Cancelled),
            item.CanTransitionTo(OrderItemStatus.Refunded));
    }

    /// <summary>
    /// Gets fulfilment summary for a sub-order.
    /// </summary>
    public async Task<SubOrderFulfilmentSummaryDto?> HandleAsync(
        GetSubOrderFulfilmentSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (shipment, _, items) = await _orderRepository.GetShipmentWithOrderAsync(
            query.ShipmentId,
            cancellationToken);

        if (shipment is null || shipment.StoreId != query.StoreId)
        {
            return null;
        }

        var newItems = items.Count(i => i.Status == OrderItemStatus.New);
        var preparingItems = items.Count(i => i.Status == OrderItemStatus.Preparing);
        var shippedItems = items.Count(i => i.Status == OrderItemStatus.Shipped);
        var deliveredItems = items.Count(i => i.Status == OrderItemStatus.Delivered);
        var cancelledItems = items.Count(i => i.Status == OrderItemStatus.Cancelled);
        var refundedItems = items.Count(i => i.Status == OrderItemStatus.Refunded);
        var totalItems = items.Count;

        // Determine partial fulfilment state
        var activeItems = totalItems - cancelledItems - refundedItems;
        var isPartiallyFulfilled = shippedItems > 0 && shippedItems < activeItems;
        var isFullyShipped = activeItems > 0 && (shippedItems + deliveredItems) == activeItems;
        var isFullyDelivered = activeItems > 0 && deliveredItems == activeItems;

        return new SubOrderFulfilmentSummaryDto(
            shipment.Id,
            shipment.Status.ToString(),
            totalItems,
            newItems,
            preparingItems,
            shippedItems,
            deliveredItems,
            cancelledItems,
            refundedItems,
            isPartiallyFulfilled,
            isFullyShipped,
            isFullyDelivered);
    }

    /// <summary>
    /// Calculates refund amounts for cancelled items.
    /// </summary>
    public async Task<PartialRefundCalculationDto?> HandleAsync(
        CalculatePartialRefundQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (shipment, order, items) = await _orderRepository.GetShipmentWithOrderAsync(
            query.ShipmentId,
            cancellationToken);

        if (shipment is null || order is null || shipment.StoreId != query.StoreId)
        {
            return null;
        }

        // Filter to specified items or all cancelled items
        var targetItems = query.ItemIds is not null && query.ItemIds.Count > 0
            ? items.Where(i => query.ItemIds.Contains(i.Id) && i.Status == OrderItemStatus.Cancelled).ToList()
            : items.Where(i => i.Status == OrderItemStatus.Cancelled).ToList();

        var breakdowns = targetItems.Select(item => new ItemRefundBreakdownDto(
            item.Id,
            item.ProductName,
            item.LineTotal,
            item.ShippingCost,
            item.GetRefundableAmount())).ToList();

        var totalRefundAmount = breakdowns.Sum(b => b.RefundAmount);

        return new PartialRefundCalculationDto(
            totalRefundAmount,
            order.Currency,
            breakdowns.AsReadOnly());
    }

    /// <summary>
    /// Updates an individual item's fulfilment status.
    /// </summary>
    public async Task<UpdateItemStatusResultDto> HandleAsync(
        UpdateItemStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var (shipment, order, items) = await _orderRepository.GetShipmentWithOrderAsync(
            command.ShipmentId,
            cancellationToken);

        if (shipment is null || order is null)
        {
            return new UpdateItemStatusResultDto(false, "Shipment not found.");
        }

        if (shipment.StoreId != command.StoreId)
        {
            return new UpdateItemStatusResultDto(false, "Shipment does not belong to this store.");
        }

        var item = items.FirstOrDefault(i => i.Id == command.ItemId);
        if (item is null)
        {
            return new UpdateItemStatusResultDto(false, "Item not found in this shipment.");
        }

        if (!Enum.TryParse<OrderItemStatus>(command.NewStatus, ignoreCase: true, out var targetStatus))
        {
            return new UpdateItemStatusResultDto(false, $"Invalid status: {command.NewStatus}.");
        }

        if (!item.CanTransitionTo(targetStatus))
        {
            return new UpdateItemStatusResultDto(
                false,
                $"Cannot transition item from {item.Status} to {targetStatus}.");
        }

        var previousStatus = item.Status.ToString();

        try
        {
            ApplyItemStatusChange(item, targetStatus, command.CarrierName, command.TrackingNumber, command.TrackingUrl);
        }
        catch (InvalidOperationException ex)
        {
            return new UpdateItemStatusResultDto(false, ex.Message);
        }

        await _orderRepository.SaveChangesAsync(cancellationToken);

        // Send notification to buyer for significant status changes
        if (targetStatus is OrderItemStatus.Shipped or OrderItemStatus.Cancelled or OrderItemStatus.Refunded)
        {
            var buyer = await _userRepository.GetByIdAsync(order.BuyerId, cancellationToken);
            if (buyer?.Email is not null)
            {
                await _notificationService.SendItemStatusChangedAsync(
                    item.Id,
                    order.Id,
                    buyer.Email.Value,
                    order.OrderNumber,
                    item.ProductName,
                    previousStatus,
                    item.Status.ToString(),
                    item.TrackingNumber,
                    item.CarrierName,
                    cancellationToken);
            }
        }

        return new UpdateItemStatusResultDto(
            true,
            null,
            previousStatus,
            item.Status.ToString());
    }

    /// <summary>
    /// Updates multiple items' status at once.
    /// </summary>
    public async Task<UpdateItemStatusResultDto> HandleAsync(
        BatchUpdateItemStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ItemIds.Count == 0)
        {
            return new UpdateItemStatusResultDto(false, "No items specified.");
        }

        var (shipment, order, items) = await _orderRepository.GetShipmentWithOrderAsync(
            command.ShipmentId,
            cancellationToken);

        if (shipment is null || order is null)
        {
            return new UpdateItemStatusResultDto(false, "Shipment not found.");
        }

        if (shipment.StoreId != command.StoreId)
        {
            return new UpdateItemStatusResultDto(false, "Shipment does not belong to this store.");
        }

        if (!Enum.TryParse<OrderItemStatus>(command.NewStatus, ignoreCase: true, out var targetStatus))
        {
            return new UpdateItemStatusResultDto(false, $"Invalid status: {command.NewStatus}.");
        }

        var targetItems = items.Where(i => command.ItemIds.Contains(i.Id)).ToList();
        if (targetItems.Count != command.ItemIds.Count)
        {
            return new UpdateItemStatusResultDto(false, "Some items were not found in this shipment.");
        }

        // Validate all transitions first
        foreach (var item in targetItems)
        {
            if (!item.CanTransitionTo(targetStatus))
            {
                return new UpdateItemStatusResultDto(
                    false,
                    $"Cannot transition item '{item.ProductName}' from {item.Status} to {targetStatus}.");
            }
        }

        // Apply all changes
        foreach (var item in targetItems)
        {
            ApplyItemStatusChange(item, targetStatus, command.CarrierName, command.TrackingNumber, command.TrackingUrl);
        }

        await _orderRepository.SaveChangesAsync(cancellationToken);

        // Send batch notification
        if (targetStatus is OrderItemStatus.Shipped or OrderItemStatus.Cancelled or OrderItemStatus.Refunded)
        {
            var buyer = await _userRepository.GetByIdAsync(order.BuyerId, cancellationToken);
            if (buyer?.Email is not null)
            {
                var itemNames = string.Join(", ", targetItems.Select(i => i.ProductName));
                await _notificationService.SendBatchItemStatusChangedAsync(
                    order.Id,
                    buyer.Email.Value,
                    order.OrderNumber,
                    targetItems.Count,
                    itemNames,
                    targetStatus.ToString(),
                    cancellationToken);
            }
        }

        return new UpdateItemStatusResultDto(
            true,
            null,
            null,
            targetStatus.ToString());
    }

    /// <summary>
    /// Cancels specific items within a sub-order.
    /// </summary>
    public async Task<UpdateItemStatusResultDto> HandleAsync(
        CancelItemsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var batchCommand = new BatchUpdateItemStatusCommand(
            command.StoreId,
            command.ShipmentId,
            command.ItemIds,
            OrderItemStatus.Cancelled.ToString(),
            command.CancelledByUserId);

        return await HandleAsync(batchCommand, cancellationToken);
    }

    /// <summary>
    /// Refunds specific cancelled items within a sub-order.
    /// </summary>
    public async Task<UpdateItemStatusResultDto> HandleAsync(
        RefundItemsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ItemIds.Count == 0)
        {
            return new UpdateItemStatusResultDto(false, "No items specified.");
        }

        var (shipment, order, items) = await _orderRepository.GetShipmentWithOrderAsync(
            command.ShipmentId,
            cancellationToken);

        if (shipment is null || order is null)
        {
            return new UpdateItemStatusResultDto(false, "Shipment not found.");
        }

        if (shipment.StoreId != command.StoreId)
        {
            return new UpdateItemStatusResultDto(false, "Shipment does not belong to this store.");
        }

        var targetItems = items.Where(i => command.ItemIds.Contains(i.Id)).ToList();
        if (targetItems.Count != command.ItemIds.Count)
        {
            return new UpdateItemStatusResultDto(false, "Some items were not found in this shipment.");
        }

        // Validate all items can be refunded
        foreach (var item in targetItems)
        {
            if (!item.CanTransitionTo(OrderItemStatus.Refunded))
            {
                return new UpdateItemStatusResultDto(
                    false,
                    $"Cannot refund item '{item.ProductName}' in status {item.Status}.");
            }
        }

        // Apply refunds
        foreach (var item in targetItems)
        {
            item.Refund();
        }

        await _orderRepository.SaveChangesAsync(cancellationToken);

        // Send notification
        var buyer = await _userRepository.GetByIdAsync(order.BuyerId, cancellationToken);
        if (buyer?.Email is not null)
        {
            var totalRefund = targetItems.Sum(i => i.RefundedAmount ?? 0);
            await _notificationService.SendItemsRefundedAsync(
                order.Id,
                buyer.Email.Value,
                order.OrderNumber,
                targetItems.Count,
                totalRefund,
                order.Currency,
                cancellationToken);
        }

        return new UpdateItemStatusResultDto(
            true,
            null,
            null,
            OrderItemStatus.Refunded.ToString());
    }

    /// <summary>
    /// Updates tracking info for specific shipped items.
    /// </summary>
    public async Task<UpdateItemStatusResultDto> HandleAsync(
        UpdateItemsTrackingCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ItemIds.Count == 0)
        {
            return new UpdateItemStatusResultDto(false, "No items specified.");
        }

        var (shipment, order, items) = await _orderRepository.GetShipmentWithOrderAsync(
            command.ShipmentId,
            cancellationToken);

        if (shipment is null || order is null)
        {
            return new UpdateItemStatusResultDto(false, "Shipment not found.");
        }

        if (shipment.StoreId != command.StoreId)
        {
            return new UpdateItemStatusResultDto(false, "Shipment does not belong to this store.");
        }

        var targetItems = items.Where(i => command.ItemIds.Contains(i.Id)).ToList();
        if (targetItems.Count != command.ItemIds.Count)
        {
            return new UpdateItemStatusResultDto(false, "Some items were not found in this shipment.");
        }

        // Validate all items are shipped
        foreach (var item in targetItems)
        {
            if (item.Status != OrderItemStatus.Shipped)
            {
                return new UpdateItemStatusResultDto(
                    false,
                    $"Cannot update tracking for item '{item.ProductName}' in status {item.Status}. Item must be shipped.");
            }
        }

        // Update tracking info
        foreach (var item in targetItems)
        {
            item.UpdateTrackingInfo(command.CarrierName, command.TrackingNumber, command.TrackingUrl);
        }

        await _orderRepository.SaveChangesAsync(cancellationToken);

        return new UpdateItemStatusResultDto(
            true,
            null,
            OrderItemStatus.Shipped.ToString(),
            OrderItemStatus.Shipped.ToString());
    }

    private static void ApplyItemStatusChange(
        OrderItem item,
        OrderItemStatus targetStatus,
        string? carrierName,
        string? trackingNumber,
        string? trackingUrl)
    {
        switch (targetStatus)
        {
            case OrderItemStatus.Preparing:
                item.MarkPreparing();
                break;
            case OrderItemStatus.Shipped:
                item.Ship(carrierName, trackingNumber, trackingUrl);
                break;
            case OrderItemStatus.Delivered:
                item.MarkDelivered();
                break;
            case OrderItemStatus.Cancelled:
                item.Cancel();
                break;
            case OrderItemStatus.Refunded:
                item.Refund();
                break;
            default:
                throw new InvalidOperationException($"Status transition to {targetStatus} is not supported.");
        }
    }
}
