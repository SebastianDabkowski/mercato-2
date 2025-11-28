using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for category persistence operations.
/// </summary>
public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Category>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Category>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Category>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default);
    Task<bool> HasChildrenAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<int> GetProductCountAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, int>> GetProductCountsAsync(IEnumerable<Guid> categoryIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, int>> GetChildCountsAsync(IEnumerable<Guid> categoryIds, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets category suggestions matching the search prefix.
    /// Only returns active categories.
    /// </summary>
    /// <param name="searchPrefix">The prefix to search for.</param>
    /// <param name="maxResults">Maximum number of suggestions to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of categories matching the prefix.</returns>
    Task<IReadOnlyCollection<Category>> GetSuggestionsAsync(
        string searchPrefix,
        int maxResults = 5,
        CancellationToken cancellationToken = default);
    
    Task AddAsync(Category category, CancellationToken cancellationToken = default);
    void Update(Category category);
    void Delete(Category category);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
