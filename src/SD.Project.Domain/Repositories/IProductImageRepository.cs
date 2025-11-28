using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for product image persistence operations.
/// </summary>
public interface IProductImageRepository
{
    Task<ProductImage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ProductImage>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ProductImage?> GetMainImageByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<int> GetImageCountByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task AddAsync(ProductImage image, CancellationToken cancellationToken = default);
    void Update(ProductImage image);
    void Delete(ProductImage image);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
