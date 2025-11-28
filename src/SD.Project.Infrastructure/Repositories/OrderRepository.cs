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
}
