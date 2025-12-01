using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core backed delivery address repository.
/// </summary>
public sealed class DeliveryAddressRepository : IDeliveryAddressRepository
{
    private readonly AppDbContext _context;

    public DeliveryAddressRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DeliveryAddress?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.DeliveryAddresses
            .FirstOrDefaultAsync(a => a.Id == id && a.IsActive, cancellationToken);
    }

    public async Task<IReadOnlyList<DeliveryAddress>> GetByBuyerIdAsync(Guid buyerId, CancellationToken cancellationToken = default)
    {
        return await _context.DeliveryAddresses
            .Where(a => a.BuyerId == buyerId && a.IsActive)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<DeliveryAddress?> GetDefaultByBuyerIdAsync(Guid buyerId, CancellationToken cancellationToken = default)
    {
        return await _context.DeliveryAddresses
            .FirstOrDefaultAsync(a => a.BuyerId == buyerId && a.IsDefault && a.IsActive, cancellationToken);
    }

    public async Task<IReadOnlyList<DeliveryAddress>> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.DeliveryAddresses
            .Where(a => a.SessionId == sessionId && a.IsActive)
            .OrderByDescending(a => a.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(DeliveryAddress address, CancellationToken cancellationToken = default)
    {
        await _context.DeliveryAddresses.AddAsync(address, cancellationToken);
    }

    public Task UpdateAsync(DeliveryAddress address, CancellationToken cancellationToken = default)
    {
        _context.DeliveryAddresses.Update(address);
        return Task.CompletedTask;
    }

    public void Remove(DeliveryAddress address)
    {
        _context.DeliveryAddresses.Remove(address);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
