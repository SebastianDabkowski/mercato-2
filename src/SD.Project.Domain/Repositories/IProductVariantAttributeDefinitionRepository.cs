using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for product variant attribute definition persistence operations.
/// </summary>
public interface IProductVariantAttributeDefinitionRepository
{
    Task<ProductVariantAttributeDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ProductVariantAttributeDefinition>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task AddAsync(ProductVariantAttributeDefinition definition, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<ProductVariantAttributeDefinition> definitions, CancellationToken cancellationToken = default);
    void Update(ProductVariantAttributeDefinition definition);
    void Delete(ProductVariantAttributeDefinition definition);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
