using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for shared attribute persistence operations.
/// </summary>
public interface ISharedAttributeRepository
{
    /// <summary>
    /// Gets a shared attribute by its ID.
    /// </summary>
    Task<SharedAttribute?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all shared attributes.
    /// </summary>
    Task<IReadOnlyCollection<SharedAttribute>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a shared attribute with the given name already exists.
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of categories using a shared attribute.
    /// </summary>
    Task<int> GetLinkedCategoryCountAsync(Guid sharedAttributeId, CancellationToken cancellationToken = default);

    Task AddAsync(SharedAttribute attribute, CancellationToken cancellationToken = default);
    void Update(SharedAttribute attribute);
    void Delete(SharedAttribute attribute);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
