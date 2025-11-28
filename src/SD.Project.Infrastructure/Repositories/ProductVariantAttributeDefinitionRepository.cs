using Microsoft.EntityFrameworkCore;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Infrastructure.Persistence;

namespace SD.Project.Infrastructure.Repositories;

/// <summary>
/// EF Core backed repository for product variant attribute definitions.
/// </summary>
public sealed class ProductVariantAttributeDefinitionRepository : IProductVariantAttributeDefinitionRepository
{
    private readonly AppDbContext _context;

    public ProductVariantAttributeDefinitionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ProductVariantAttributeDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ProductVariantAttributeDefinitions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductVariantAttributeDefinition>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var results = await _context.ProductVariantAttributeDefinitions
            .AsNoTracking()
            .Where(d => d.ProductId == productId)
            .OrderBy(d => d.DisplayOrder)
            .ToListAsync(cancellationToken);
        return results.AsReadOnly();
    }

    public async Task AddAsync(ProductVariantAttributeDefinition definition, CancellationToken cancellationToken = default)
    {
        await _context.ProductVariantAttributeDefinitions.AddAsync(definition, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<ProductVariantAttributeDefinition> definitions, CancellationToken cancellationToken = default)
    {
        await _context.ProductVariantAttributeDefinitions.AddRangeAsync(definitions, cancellationToken);
    }

    public void Update(ProductVariantAttributeDefinition definition)
    {
        _context.ProductVariantAttributeDefinitions.Update(definition);
    }

    public void Delete(ProductVariantAttributeDefinition definition)
    {
        _context.ProductVariantAttributeDefinitions.Remove(definition);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
