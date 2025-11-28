using SD.Project.Domain.Entities;
using SD.Project.Domain.ValueObjects;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Repository contract for internal user operations.
/// </summary>
public interface IInternalUserRepository
{
    /// <summary>
    /// Gets an internal user by their ID.
    /// </summary>
    Task<InternalUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an internal user by store ID and user ID.
    /// </summary>
    Task<InternalUser?> GetByStoreAndUserIdAsync(Guid storeId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an internal user by store ID and email.
    /// </summary>
    Task<InternalUser?> GetByStoreAndEmailAsync(Guid storeId, Email email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all internal users for a store.
    /// </summary>
    Task<IReadOnlyList<InternalUser>> GetByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active internal users for a store.
    /// </summary>
    Task<IReadOnlyList<InternalUser>> GetActiveByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an email is already associated with the store.
    /// </summary>
    Task<bool> ExistsByStoreAndEmailAsync(Guid storeId, Email email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new internal user.
    /// </summary>
    Task AddAsync(InternalUser internalUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists changes to the underlying store.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
