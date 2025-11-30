using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of shipment status history persistence.
/// </summary>
public sealed class ShipmentStatusHistoryRepository : IShipmentStatusHistoryRepository
{
    private readonly AppDbContext _context;

    public ShipmentStatusHistoryRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task AddAsync(ShipmentStatusHistory history, CancellationToken cancellationToken = default)
    {
        await _context.ShipmentStatusHistories.AddAsync(history, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShipmentStatusHistory>> GetByShipmentIdAsync(
        Guid shipmentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ShipmentStatusHistories
            .AsNoTracking()
            .Where(h => h.ShipmentId == shipmentId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShipmentStatusHistory>> GetByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ShipmentStatusHistories
            .AsNoTracking()
            .Where(h => h.OrderId == orderId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
