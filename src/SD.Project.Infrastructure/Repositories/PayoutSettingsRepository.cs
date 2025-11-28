using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of IPayoutSettingsRepository.
/// </summary>
public sealed class PayoutSettingsRepository : IPayoutSettingsRepository
{
    private readonly AppDbContext _context;

    public PayoutSettingsRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PayoutSettings?> GetBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default)
    {
        return await _context.PayoutSettings
            .FirstOrDefaultAsync(p => p.SellerId == sellerId, cancellationToken);
    }

    public async Task AddAsync(PayoutSettings settings, CancellationToken cancellationToken = default)
    {
        await _context.PayoutSettings.AddAsync(settings, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
