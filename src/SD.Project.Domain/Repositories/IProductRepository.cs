using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for product persistence operations.
/// </summary>
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Product>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Product>> GetByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Product>> GetAllByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<Product?> GetBySkuAndStoreIdAsync(string sku, Guid storeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Product>> GetBySkusAndStoreIdAsync(IEnumerable<string> skus, Guid storeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Product>> GetByCategoryAsync(string categoryName, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Product>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Filters products by multiple criteria including search term, category, price range, condition, and store.
    /// </summary>
    /// <param name="searchTerm">Optional text to search in name and description.</param>
    /// <param name="category">Optional category name filter.</param>
    /// <param name="minPrice">Optional minimum price (inclusive).</param>
    /// <param name="maxPrice">Optional maximum price (inclusive).</param>
    /// <param name="condition">Optional product status filter.</param>
    /// <param name="storeId">Optional store ID filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of products matching all specified criteria.</returns>
    Task<IReadOnlyCollection<Product>> FilterAsync(
        string? searchTerm = null,
        string? category = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        ProductStatus? condition = null,
        Guid? storeId = null,
        CancellationToken cancellationToken = default);
    
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Product> products, CancellationToken cancellationToken = default);
    void Update(Product product);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
