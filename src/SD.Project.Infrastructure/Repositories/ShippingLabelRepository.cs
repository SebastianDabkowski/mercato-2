using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for shipping label operations.
/// </summary>
public sealed class ShippingLabelRepository : IShippingLabelRepository
{
    private readonly AppDbContext _context;

    public ShippingLabelRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<ShippingLabel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ShippingLabels
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ShippingLabel?> GetByShipmentIdAsync(Guid shipmentId, CancellationToken cancellationToken = default)
    {
        return await _context.ShippingLabels
            .Where(l => l.ShipmentId == shipmentId && !l.IsVoided)
            .OrderByDescending(l => l.GeneratedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingLabel>> GetAllByShipmentIdAsync(Guid shipmentId, CancellationToken cancellationToken = default)
    {
        return await _context.ShippingLabels
            .Where(l => l.ShipmentId == shipmentId)
            .OrderByDescending(l => l.GeneratedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingLabel>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.ShippingLabels
            .Where(l => l.OrderId == orderId && !l.IsVoided)
            .OrderByDescending(l => l.GeneratedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingLabel>> GetExpiredLabelsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        return await _context.ShippingLabels
            .Where(l => l.ExpiresAt.HasValue && l.ExpiresAt.Value < cutoffDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(ShippingLabel label, CancellationToken cancellationToken = default)
    {
        await _context.ShippingLabels.AddAsync(label, cancellationToken);
    }

    /// <inheritdoc />
    public Task UpdateAsync(ShippingLabel label, CancellationToken cancellationToken = default)
    {
        _context.ShippingLabels.Update(label);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(ShippingLabel label, CancellationToken cancellationToken = default)
    {
        _context.ShippingLabels.Remove(label);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
