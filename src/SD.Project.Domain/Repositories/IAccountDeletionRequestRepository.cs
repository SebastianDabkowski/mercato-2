using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for account deletion request persistence operations.
/// </summary>
public interface IAccountDeletionRequestRepository
{
    /// <summary>
    /// Gets an account deletion request by ID.
    /// </summary>
    Task<AccountDeletionRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent pending deletion request for a user.
    /// </summary>
    Task<AccountDeletionRequest?> GetPendingByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all deletion requests for a user.
    /// </summary>
    Task<IReadOnlyList<AccountDeletionRequest>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the user has a pending or processing deletion request.
    /// </summary>
    Task<bool> HasActiveDeletionRequestAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new deletion request.
    /// </summary>
    Task AddAsync(AccountDeletionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing deletion request.
    /// </summary>
    void Update(AccountDeletionRequest request);

    /// <summary>
    /// Adds an audit log entry.
    /// </summary>
    Task AddAuditLogAsync(AccountDeletionAuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific deletion request.
    /// </summary>
    Task<IReadOnlyList<AccountDeletionAuditLog>> GetAuditLogsForRequestAsync(Guid deletionRequestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific user (affected or triggered).
    /// </summary>
    Task<IReadOnlyList<AccountDeletionAuditLog>> GetAuditLogsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
