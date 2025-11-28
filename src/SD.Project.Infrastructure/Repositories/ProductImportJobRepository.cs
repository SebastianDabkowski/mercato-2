using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core backed repository for product import jobs.
/// </summary>
public sealed class ProductImportJobRepository : IProductImportJobRepository
{
    private readonly AppDbContext _context;

    public ProductImportJobRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ProductImportJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ProductImportJobs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductImportJob>> GetByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        var results = await _context.ProductImportJobs
            .AsNoTracking()
            .Where(j => j.StoreId == storeId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task AddAsync(ProductImportJob job, CancellationToken cancellationToken = default)
    {
        await _context.ProductImportJobs.AddAsync(job, cancellationToken);
    }

    public void Update(ProductImportJob job)
    {
        _context.ProductImportJobs.Update(job);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
