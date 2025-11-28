using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for category persistence operations.
/// </summary>
public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Category>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Category>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Category>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default);
    Task<bool> HasChildrenAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<int> GetProductCountAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, int>> GetProductCountsAsync(IEnumerable<Guid> categoryIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, int>> GetChildCountsAsync(IEnumerable<Guid> categoryIds, CancellationToken cancellationToken = default);
    Task AddAsync(Category category, CancellationToken cancellationToken = default);
    void Update(Category category);
    void Delete(Category category);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
