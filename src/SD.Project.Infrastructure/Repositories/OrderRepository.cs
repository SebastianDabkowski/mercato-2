using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of order persistence.
/// </summary>
public sealed class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order is not null)
        {
            var items = await _context.OrderItems
                .Where(i => i.OrderId == id)
                .ToListAsync(cancellationToken);
            order.LoadItems(items);

            var shipments = await _context.OrderShipments
                .Where(s => s.OrderId == id)
                .ToListAsync(cancellationToken);
            order.LoadShipments(shipments);
        }

        return order;
    }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);

        if (order is not null)
        {
            var items = await _context.OrderItems
                .Where(i => i.OrderId == order.Id)
                .ToListAsync(cancellationToken);
            order.LoadItems(items);

            var shipments = await _context.OrderShipments
                .Where(s => s.OrderId == order.Id)
                .ToListAsync(cancellationToken);
            order.LoadShipments(shipments);
        }

        return order;
    }

    public async Task<IReadOnlyList<Order>> GetByBuyerIdAsync(Guid buyerId, CancellationToken cancellationToken = default)
    {
        var orders = await _context.Orders
            .Where(o => o.BuyerId == buyerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        // Load items for each order
        var orderIds = orders.Select(o => o.Id).ToList();
        var allItems = await _context.OrderItems
            .Where(i => orderIds.Contains(i.OrderId))
            .ToListAsync(cancellationToken);

        var itemsByOrder = allItems.GroupBy(i => i.OrderId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var order in orders)
        {
            if (itemsByOrder.TryGetValue(order.Id, out var items))
            {
                order.LoadItems(items);
            }
        }

        return orders.AsReadOnly();
    }

    public async Task<IReadOnlyList<Order>> GetRecentByBuyerIdAsync(
        Guid buyerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var orders = await _context.Orders
            .Where(o => o.BuyerId == buyerId)
            .OrderByDescending(o => o.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        // Load items for each order
        var orderIds = orders.Select(o => o.Id).ToList();
        var allItems = await _context.OrderItems
            .Where(i => orderIds.Contains(i.OrderId))
            .ToListAsync(cancellationToken);

        var itemsByOrder = allItems.GroupBy(i => i.OrderId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var order in orders)
        {
            if (itemsByOrder.TryGetValue(order.Id, out var items))
            {
                order.LoadItems(items);
            }
        }

        return orders.AsReadOnly();
    }

    public async Task<(IReadOnlyList<Order> Orders, int TotalCount)> GetFilteredByBuyerIdAsync(
        Guid buyerId,
        OrderStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        Guid? sellerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        // Start with orders for the buyer
        var ordersQuery = _context.Orders
            .Where(o => o.BuyerId == buyerId);

        // Apply status filter
        if (status.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.Status == status.Value);
        }

        // Apply date range filter
        if (fromDate.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.CreatedAt >= fromDate.Value);
        }
        if (toDate.HasValue)
        {
            // Include the entire end date
            var endOfDay = toDate.Value.Date.AddDays(1);
            ordersQuery = ordersQuery.Where(o => o.CreatedAt < endOfDay);
        }

        // For seller filter, we need to find orders that have items from that store
        if (sellerId.HasValue)
        {
            var orderIdsWithSeller = await _context.OrderItems
                .Where(i => i.StoreId == sellerId.Value)
                .Select(i => i.OrderId)
                .Distinct()
                .ToListAsync(cancellationToken);

            ordersQuery = ordersQuery.Where(o => orderIdsWithSeller.Contains(o.Id));
        }

        // Get total count before pagination
        var totalCount = await ordersQuery.CountAsync(cancellationToken);

        // Apply pagination and ordering
        var orders = await ordersQuery
            .OrderByDescending(o => o.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        // Load items and shipments for each order
        if (orders.Count > 0)
        {
            var orderIds = orders.Select(o => o.Id).ToList();
            var allItems = await _context.OrderItems
                .Where(i => orderIds.Contains(i.OrderId))
                .ToListAsync(cancellationToken);

            var itemsByOrder = allItems.GroupBy(i => i.OrderId).ToDictionary(g => g.Key, g => g.ToList());

            var allShipments = await _context.OrderShipments
                .Where(s => orderIds.Contains(s.OrderId))
                .ToListAsync(cancellationToken);

            var shipmentsByOrder = allShipments.GroupBy(s => s.OrderId).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var order in orders)
            {
                if (itemsByOrder.TryGetValue(order.Id, out var items))
                {
                    order.LoadItems(items);
                }

                if (shipmentsByOrder.TryGetValue(order.Id, out var shipments))
                {
                    order.LoadShipments(shipments);
                }
            }
        }

        return (orders.AsReadOnly(), totalCount);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _context.Orders.AddAsync(order, cancellationToken);

        // Add items
        foreach (var item in order.Items)
        {
            await _context.OrderItems.AddAsync(item, cancellationToken);
        }

        // Add shipments
        foreach (var shipment in order.Shipments)
        {
            await _context.OrderShipments.AddAsync(shipment, cancellationToken);
        }
    }

    public Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        _context.Orders.Update(order);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken = default)
    {
        // Generate a unique order number: MKT-YYYYMMDD-XXXXX
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var randomPart = Guid.NewGuid().ToString("N")[..5].ToUpperInvariant();
        return Task.FromResult($"MKT-{datePart}-{randomPart}");
    }

    public async Task<IReadOnlyList<OrderShipment>> GetShipmentsByStoreIdAsync(
        Guid storeId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var shipments = await _context.OrderShipments
            .Where(s => s.StoreId == storeId)
            .OrderByDescending(s => s.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return shipments.AsReadOnly();
    }

    public async Task<(OrderShipment? Shipment, Order? Order, IReadOnlyList<OrderItem> Items)> GetShipmentWithOrderAsync(
        Guid shipmentId,
        CancellationToken cancellationToken = default)
    {
        var shipment = await _context.OrderShipments
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

        if (shipment is null)
        {
            return (null, null, Array.Empty<OrderItem>());
        }

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == shipment.OrderId, cancellationToken);

        // Get only items for this shipment's store
        var items = await _context.OrderItems
            .Where(i => i.OrderId == shipment.OrderId && i.StoreId == shipment.StoreId)
            .ToListAsync(cancellationToken);

        return (shipment, order, items.AsReadOnly());
    }

    public async Task<bool> ExistsByPaymentTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
        {
            return false;
        }

        return await _context.Orders
            .AnyAsync(o => o.PaymentTransactionId == transactionId, cancellationToken);
    }

    public async Task<(IReadOnlyList<OrderShipment> Shipments, int TotalCount)> GetFilteredShipmentsByStoreIdAsync(
        Guid storeId,
        ShipmentStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        string? buyerSearch,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        // Start with shipments for the store
        var shipmentsQuery = _context.OrderShipments
            .Where(s => s.StoreId == storeId);

        // Apply status filter
        if (status.HasValue)
        {
            shipmentsQuery = shipmentsQuery.Where(s => s.Status == status.Value);
        }

        // Apply date range filter
        if (fromDate.HasValue)
        {
            shipmentsQuery = shipmentsQuery.Where(s => s.CreatedAt >= fromDate.Value);
        }
        if (toDate.HasValue)
        {
            // Include the entire end date
            var endOfDay = toDate.Value.Date.AddDays(1);
            shipmentsQuery = shipmentsQuery.Where(s => s.CreatedAt < endOfDay);
        }

        // For buyer search, we need to join with orders
        if (!string.IsNullOrWhiteSpace(buyerSearch))
        {
            var searchTerm = buyerSearch.Trim().ToLowerInvariant();
            var orderIds = await _context.Orders
                .Where(o => o.RecipientName.ToLower().Contains(searchTerm) ||
                           o.OrderNumber.ToLower().Contains(searchTerm))
                .Select(o => o.Id)
                .ToListAsync(cancellationToken);

            shipmentsQuery = shipmentsQuery.Where(s => orderIds.Contains(s.OrderId));
        }

        // Get total count before pagination
        var totalCount = await shipmentsQuery.CountAsync(cancellationToken);

        // Apply pagination and ordering
        var shipments = await shipmentsQuery
            .OrderByDescending(s => s.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (shipments.AsReadOnly(), totalCount);
    }

    public async Task<IReadOnlyList<(OrderShipment Shipment, Order Order, IReadOnlyList<OrderItem> Items)>> GetAllShipmentsForExportAsync(
        Guid storeId,
        ShipmentStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        string? buyerSearch,
        CancellationToken cancellationToken = default)
    {
        // Start with shipments for the store
        var shipmentsQuery = _context.OrderShipments
            .Where(s => s.StoreId == storeId);

        // Apply status filter
        if (status.HasValue)
        {
            shipmentsQuery = shipmentsQuery.Where(s => s.Status == status.Value);
        }

        // Apply date range filter
        if (fromDate.HasValue)
        {
            shipmentsQuery = shipmentsQuery.Where(s => s.CreatedAt >= fromDate.Value);
        }
        if (toDate.HasValue)
        {
            var endOfDay = toDate.Value.Date.AddDays(1);
            shipmentsQuery = shipmentsQuery.Where(s => s.CreatedAt < endOfDay);
        }

        // Get all matching shipments
        var shipments = await shipmentsQuery
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

        if (shipments.Count == 0)
        {
            return Array.Empty<(OrderShipment, Order, IReadOnlyList<OrderItem>)>();
        }

        // Get all related orders
        var orderIds = shipments.Select(s => s.OrderId).Distinct().ToList();
        var orders = await _context.Orders
            .Where(o => orderIds.Contains(o.Id))
            .ToListAsync(cancellationToken);
        var ordersDict = orders.ToDictionary(o => o.Id);

        // Apply buyer search filter if specified
        if (!string.IsNullOrWhiteSpace(buyerSearch))
        {
            var searchTerm = buyerSearch.Trim().ToLowerInvariant();
            var matchingOrderIds = orders
                .Where(o => o.RecipientName.ToLowerInvariant().Contains(searchTerm) ||
                           o.OrderNumber.ToLowerInvariant().Contains(searchTerm))
                .Select(o => o.Id)
                .ToHashSet();
            
            shipments = shipments.Where(s => matchingOrderIds.Contains(s.OrderId)).ToList();
            
            if (shipments.Count == 0)
            {
                return Array.Empty<(OrderShipment, Order, IReadOnlyList<OrderItem>)>();
            }
        }

        // Get all items for the shipments (store-specific)
        var items = await _context.OrderItems
            .Where(i => orderIds.Contains(i.OrderId) && i.StoreId == storeId)
            .ToListAsync(cancellationToken);
        var itemsByOrder = items.GroupBy(i => i.OrderId).ToDictionary(g => g.Key, g => g.ToList());

        // Build result
        var result = new List<(OrderShipment Shipment, Order Order, IReadOnlyList<OrderItem> Items)>();
        foreach (var shipment in shipments)
        {
            if (ordersDict.TryGetValue(shipment.OrderId, out var order))
            {
                var shipmentItems = itemsByOrder.TryGetValue(shipment.OrderId, out var orderItems)
                    ? orderItems.AsReadOnly()
                    : (IReadOnlyList<OrderItem>)Array.Empty<OrderItem>();
                result.Add((shipment, order, shipmentItems));
            }
        }

        return result.AsReadOnly();
    }
}
