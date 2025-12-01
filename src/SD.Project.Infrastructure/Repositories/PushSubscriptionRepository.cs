using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core backed repository for push subscriptions.
/// </summary>
public sealed class PushSubscriptionRepository : IPushSubscriptionRepository
{
    private readonly AppDbContext _context;

    public PushSubscriptionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PushSubscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PushSubscriptions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<PushSubscription?> GetByEndpointAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        return await _context.PushSubscriptions.FirstOrDefaultAsync(x => x.Endpoint == endpoint, cancellationToken);
    }

    public async Task<IReadOnlyList<PushSubscription>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.PushSubscriptions
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PushSubscription>> GetEnabledByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.PushSubscriptions
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.IsEnabled)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PushSubscription subscription, CancellationToken cancellationToken = default)
    {
        await _context.PushSubscriptions.AddAsync(subscription, cancellationToken);
    }

    public void Update(PushSubscription subscription)
    {
        _context.PushSubscriptions.Update(subscription);
    }

    public void Delete(PushSubscription subscription)
    {
        _context.PushSubscriptions.Remove(subscription);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
