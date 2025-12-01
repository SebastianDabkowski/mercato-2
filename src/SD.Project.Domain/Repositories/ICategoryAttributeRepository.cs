using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for category attribute persistence operations.
/// </summary>
public interface ICategoryAttributeRepository
{
    /// <summary>
    /// Gets an attribute by its ID.
    /// </summary>
    Task<CategoryAttribute?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all attributes for a specific category.
    /// </summary>
    Task<IReadOnlyCollection<CategoryAttribute>> GetByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets only active (non-deprecated) attributes for a specific category.
    /// </summary>
    Task<IReadOnlyCollection<CategoryAttribute>> GetActiveByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all attributes linked to a specific shared attribute.
    /// </summary>
    Task<IReadOnlyCollection<CategoryAttribute>> GetBySharedAttributeIdAsync(Guid sharedAttributeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an attribute with the given name already exists for a category.
    /// </summary>
    Task<bool> ExistsByNameAsync(Guid categoryId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of attributes for a category.
    /// </summary>
    Task<int> GetCountByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default);

    Task AddAsync(CategoryAttribute attribute, CancellationToken cancellationToken = default);
    void Update(CategoryAttribute attribute);
    void Delete(CategoryAttribute attribute);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
