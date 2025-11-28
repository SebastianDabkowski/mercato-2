using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for product variant persistence operations.
/// </summary>
public interface IProductVariantRepository
{
    Task<ProductVariant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ProductVariant>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ProductVariant>> GetAvailableByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ProductVariant?> GetBySkuAndProductIdAsync(string sku, Guid productId, CancellationToken cancellationToken = default);
    Task AddAsync(ProductVariant variant, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<ProductVariant> variants, CancellationToken cancellationToken = default);
    void Update(ProductVariant variant);
    void Delete(ProductVariant variant);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
