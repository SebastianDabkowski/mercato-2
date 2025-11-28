using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Repository interface for managing user sessions.
/// </summary>
public interface IUserSessionRepository
{
    /// <summary>
    /// Gets a session by its secure token.
    /// </summary>
    /// <param name="token">The session token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The session if found, null otherwise.</returns>
    Task<UserSession?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active sessions for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active sessions.</returns>
    Task<IReadOnlyList<UserSession>> GetActiveSessionsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new session.
    /// </summary>
    /// <param name="session">The session to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(UserSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing session.
    /// </summary>
    /// <param name="session">The session to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(UserSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves changes to the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes expired and revoked sessions older than the specified age.
    /// This is for cleanup purposes.
    /// </summary>
    /// <param name="olderThan">Remove sessions older than this duration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of sessions removed.</returns>
    Task<int> CleanupExpiredSessionsAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);
}
