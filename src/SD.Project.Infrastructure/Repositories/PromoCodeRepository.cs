using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing promo codes.
/// </summary>
public class PromoCodeRepository : IPromoCodeRepository
{
    private readonly AppDbContext _context;

    public PromoCodeRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<PromoCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.ToUpperInvariant().Trim();
        return await _context.PromoCodes
            .FirstOrDefaultAsync(p => p.Code == normalizedCode, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PromoCode?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PromoCodes
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetUserUsageCountAsync(Guid promoCodeId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.PromoCodeUsages
            .CountAsync(u => u.PromoCodeId == promoCodeId && u.UserId == userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RecordUsageAsync(Guid promoCodeId, Guid userId, Guid orderId, decimal discountAmount, CancellationToken cancellationToken = default)
    {
        var usage = new PromoCodeUsage(promoCodeId, userId, orderId, discountAmount);
        await _context.PromoCodeUsages.AddAsync(usage, cancellationToken);
    }

    /// <inheritdoc />
    public Task UpdateAsync(PromoCode promoCode, CancellationToken cancellationToken = default)
    {
        _context.PromoCodes.Update(promoCode);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task AddAsync(PromoCode promoCode, CancellationToken cancellationToken = default)
    {
        await _context.PromoCodes.AddAsync(promoCode, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
