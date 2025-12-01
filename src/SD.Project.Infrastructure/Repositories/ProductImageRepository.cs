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

    public async Task<(IReadOnlyCollection<ProductImage> Items, int TotalCount)> GetByModerationStatusPagedAsync(
        PhotoModerationStatus? status,
        bool? isFlagged,
        string? searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ProductImages.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(i => i.ModerationStatus == status.Value);
        }

        if (isFlagged.HasValue)
        {
            query = query.Where(i => i.IsFlagged == isFlagged.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            query = query.Where(i => i.FileName.ToLower().Contains(searchLower));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(i => i.IsFlagged)
            .ThenByDescending(i => i.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyCollection<ProductImage>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await _context.ProductImages
            .Where(i => idList.Contains(i.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductImage>> GetVisibleByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductImages
            .Where(i => i.ProductId == productId && i.ModerationStatus != PhotoModerationStatus.Removed)
            .OrderBy(i => i.DisplayOrder)
            .ThenBy(i => i.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
