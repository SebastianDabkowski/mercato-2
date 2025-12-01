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

    /// <summary>
    /// Gets images by moderation status with pagination.
    /// </summary>
    Task<(IReadOnlyCollection<ProductImage> Items, int TotalCount)> GetByModerationStatusPagedAsync(
        PhotoModerationStatus? status,
        bool? isFlagged,
        string? searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets images by multiple IDs.
    /// </summary>
    Task<IReadOnlyCollection<ProductImage>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets visible (not removed) images for a product.
    /// </summary>
    Task<IReadOnlyCollection<ProductImage>> GetVisibleByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
}
