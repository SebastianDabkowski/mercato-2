using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Repository contract for UserBlockInfo persistence operations.
/// </summary>
public interface IUserBlockInfoRepository
{
    /// <summary>
    /// Gets a block info record by its ID.
    /// </summary>
    Task<UserBlockInfo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the currently active block info for a user, if any.
    /// </summary>
    Task<UserBlockInfo?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all block info records for a user (including historical).
    /// </summary>
    Task<IReadOnlyList<UserBlockInfo>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new block info record.
    /// </summary>
    Task AddAsync(UserBlockInfo blockInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists changes to the underlying store.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
