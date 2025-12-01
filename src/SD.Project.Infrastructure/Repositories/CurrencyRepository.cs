using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of the currency repository.
/// </summary>
public class CurrencyRepository : ICurrencyRepository
{
    private readonly AppDbContext _context;

    public CurrencyRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Currency?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Currencies
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Currency?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.ToUpperInvariant();
        return await _context.Currencies
            .FirstOrDefaultAsync(c => c.Code == normalizedCode, cancellationToken);
    }

    public async Task<IReadOnlyList<Currency>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Currencies
            .OrderByDescending(c => c.IsBaseCurrency)
            .ThenBy(c => c.DisplayOrder)
            .ThenBy(c => c.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Currency>> GetEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Currencies
            .Where(c => c.IsEnabled)
            .OrderByDescending(c => c.IsBaseCurrency)
            .ThenBy(c => c.DisplayOrder)
            .ThenBy(c => c.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<Currency?> GetBaseCurrencyAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Currencies
            .FirstOrDefaultAsync(c => c.IsBaseCurrency, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.ToUpperInvariant();
        return await _context.Currencies
            .AnyAsync(c => c.Code == normalizedCode, cancellationToken);
    }

    public async Task AddAsync(Currency currency, CancellationToken cancellationToken = default)
    {
        await _context.Currencies.AddAsync(currency, cancellationToken);
    }

    public Task UpdateAsync(Currency currency, CancellationToken cancellationToken = default)
    {
        _context.Currencies.Update(currency);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
