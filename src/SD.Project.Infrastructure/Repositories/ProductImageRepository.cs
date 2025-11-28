using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of the product image repository.
/// </summary>
public sealed class ProductImageRepository : IProductImageRepository
{
    private readonly AppDbContext _context;

    public ProductImageRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ProductImage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ProductImages
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductImage>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductImages
            .Where(i => i.ProductId == productId)
            .OrderBy(i => i.DisplayOrder)
            .ThenBy(i => i.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductImage?> GetMainImageByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductImages
            .FirstOrDefaultAsync(i => i.ProductId == productId && i.IsMain, cancellationToken);
    }

    public async Task<int> GetImageCountByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductImages
            .CountAsync(i => i.ProductId == productId, cancellationToken);
    }

    public async Task AddAsync(ProductImage image, CancellationToken cancellationToken = default)
    {
        await _context.ProductImages.AddAsync(image, cancellationToken);
    }

    public void Update(ProductImage image)
    {
        _context.ProductImages.Update(image);
    }

    public void Delete(ProductImage image)
    {
        _context.ProductImages.Remove(image);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
