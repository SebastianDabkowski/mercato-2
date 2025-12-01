using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of shipping provider repository.
/// </summary>
public sealed class ShippingProviderRepository : IShippingProviderRepository
{
    private readonly AppDbContext _context;

    public ShippingProviderRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<ShippingProvider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ShippingProviders
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingProvider>> GetByStoreIdAsync(
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ShippingProviders
            .Where(p => p.StoreId == storeId)
            .OrderBy(p => p.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingProvider>> GetEnabledByStoreIdAsync(
        Guid storeId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ShippingProviders
            .Where(p => p.StoreId == storeId && p.IsEnabled)
            .OrderBy(p => p.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingProvider>> GetPlatformProvidersAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.ShippingProviders
            .Where(p => p.StoreId == null)
            .OrderBy(p => p.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ShippingProvider?> GetByStoreAndTypeAsync(
        Guid storeId,
        ShippingProviderType providerType,
        CancellationToken cancellationToken = default)
    {
        return await _context.ShippingProviders
            .FirstOrDefaultAsync(
                p => p.StoreId == storeId && p.ProviderType == providerType,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(ShippingProvider provider, CancellationToken cancellationToken = default)
    {
        await _context.ShippingProviders.AddAsync(provider, cancellationToken);
    }

    /// <inheritdoc />
    public Task UpdateAsync(ShippingProvider provider, CancellationToken cancellationToken = default)
    {
        _context.ShippingProviders.Update(provider);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(ShippingProvider provider, CancellationToken cancellationToken = default)
    {
        _context.ShippingProviders.Remove(provider);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
