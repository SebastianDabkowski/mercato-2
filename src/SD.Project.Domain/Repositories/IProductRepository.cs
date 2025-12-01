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
    /// Gets product name suggestions matching the search prefix.
    /// Only returns distinct product names from Active products.
    /// </summary>
    /// <param name="searchPrefix">The prefix to search for.</param>
    /// <param name="maxResults">Maximum number of suggestions to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of product names matching the prefix.</returns>
    Task<IReadOnlyCollection<string>> GetProductSuggestionsAsync(
        string searchPrefix,
        int maxResults = 5,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Filters active products by multiple criteria including search term, category, price range, and store.
    /// Only returns Active products for public views.
    /// </summary>
    /// <param name="searchTerm">Optional text to search in name and description.</param>
    /// <param name="category">Optional category name filter.</param>
    /// <param name="minPrice">Optional minimum price (inclusive).</param>
    /// <param name="maxPrice">Optional maximum price (inclusive).</param>
    /// <param name="storeId">Optional store ID filter.</param>
    /// <param name="sortOrder">Sort order for results. Defaults to Newest.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of active products matching all specified criteria.</returns>
    Task<IReadOnlyCollection<Product>> FilterAsync(
        string? searchTerm = null,
        string? category = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        Guid? storeId = null,
        ProductSortOrder sortOrder = ProductSortOrder.Newest,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Filters active products with pagination support.
    /// Only returns Active products for public views.
    /// </summary>
    /// <param name="searchTerm">Optional text to search in name and description.</param>
    /// <param name="category">Optional category name filter.</param>
    /// <param name="minPrice">Optional minimum price (inclusive).</param>
    /// <param name="maxPrice">Optional maximum price (inclusive).</param>
    /// <param name="storeId">Optional store ID filter.</param>
    /// <param name="sortOrder">Sort order for results. Defaults to Newest.</param>
    /// <param name="pageNumber">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple of (items on current page, total count of matching products).</returns>
    Task<(IReadOnlyCollection<Product> Items, int TotalCount)> FilterPagedAsync(
        string? searchTerm = null,
        string? category = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        Guid? storeId = null,
        ProductSortOrder sortOrder = ProductSortOrder.Newest,
        int pageNumber = 1,
        int pageSize = 12,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets products filtered by moderation status with pagination.
    /// Used by admin moderation queue.
    /// </summary>
    /// <param name="moderationStatus">Optional moderation status filter.</param>
    /// <param name="category">Optional category filter.</param>
    /// <param name="searchTerm">Optional text to search in name and description.</param>
    /// <param name="pageNumber">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple of (items on current page, total count of matching products).</returns>
    Task<(IReadOnlyCollection<Product> Items, int TotalCount)> GetByModerationStatusPagedAsync(
        ProductModerationStatus? moderationStatus = null,
        string? category = null,
        string? searchTerm = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
    
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Product> products, CancellationToken cancellationToken = default);
    void Update(Product product);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
