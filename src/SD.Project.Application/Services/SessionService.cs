using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for managing user sessions with secure tokens.
/// Provides session creation, validation, and revocation capabilities.
/// </summary>
public sealed class SessionService
{
    private readonly IUserSessionRepository _sessionRepository;

    // Default session durations
    private static readonly TimeSpan DefaultSessionDuration = TimeSpan.FromHours(24);
    private static readonly TimeSpan PersistentSessionDuration = TimeSpan.FromDays(30);
    private static readonly TimeSpan SlidingExpirationWindow = TimeSpan.FromHours(2);

    public SessionService(IUserSessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
    }

    /// <summary>
    /// Creates a new session for a user after successful authentication.
    /// </summary>
    /// <param name="userId">The authenticated user's ID.</param>
    /// <param name="isPersistent">Whether to create a persistent (Remember Me) session.</param>
    /// <param name="userAgent">The client's user agent string.</param>
    /// <param name="ipAddress">The client's IP address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created session with the secure token.</returns>
    public async Task<UserSession> CreateSessionAsync(
        Guid userId,
        bool isPersistent,
        string? userAgent = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        var duration = isPersistent ? PersistentSessionDuration : DefaultSessionDuration;
        var session = new UserSession(userId, duration, isPersistent, userAgent, ipAddress);

        await _sessionRepository.AddAsync(session, cancellationToken);
        await _sessionRepository.SaveChangesAsync(cancellationToken);

        return session;
    }

    /// <summary>
    /// Validates a session token and returns the session if valid.
    /// Also handles sliding expiration by extending the session if needed.
    /// </summary>
    /// <param name="token">The session token to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The valid session, or null if the token is invalid or expired.</returns>
    public async Task<UserSession?> ValidateSessionAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        var session = await _sessionRepository.GetByTokenAsync(token, cancellationToken);

        if (session is null || !session.IsValid)
        {
            return null;
        }

        return session;
    }

    /// <summary>
    /// Updates the last activity time of a session.
    /// This is used for session activity tracking.
    /// </summary>
    /// <param name="token">The session token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task UpdateSessionActivityAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        var session = await _sessionRepository.GetByTokenAsync(token, cancellationToken);

        if (session is null || !session.IsValid)
        {
            return;
        }

        // Check if we need to apply sliding expiration for persistent sessions
        var timeSinceLastActivity = DateTime.UtcNow - session.LastActivityAt;
        if (timeSinceLastActivity >= SlidingExpirationWindow && session.IsPersistent)
        {
            // Extend the session for persistent sessions (also updates LastActivityAt)
            session.ExtendExpiration(PersistentSessionDuration);
            await _sessionRepository.UpdateAsync(session, cancellationToken);
            await _sessionRepository.SaveChangesAsync(cancellationToken);
        }
        else if (timeSinceLastActivity >= TimeSpan.FromMinutes(5))
        {
            // Update activity timestamp periodically for all sessions (every 5 minutes)
            // This avoids excessive database writes on every request
            session.UpdateActivity();
            await _sessionRepository.UpdateAsync(session, cancellationToken);
            await _sessionRepository.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Revokes a session, making it invalid for future requests.
    /// Used when a user logs out.
    /// </summary>
    /// <param name="token">The session token to revoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the session was found and revoked, false otherwise.</returns>
    public async Task<bool> RevokeSessionAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        var session = await _sessionRepository.GetByTokenAsync(token, cancellationToken);

        if (session is null)
        {
            return false;
        }

        session.Revoke();
        await _sessionRepository.UpdateAsync(session, cancellationToken);
        await _sessionRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Revokes all sessions for a user.
    /// Used for security purposes (e.g., password change, account compromise).
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of sessions revoked.</returns>
    public async Task<int> RevokeAllUserSessionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return 0;
        }

        var sessions = await _sessionRepository.GetActiveSessionsByUserIdAsync(userId, cancellationToken);

        foreach (var session in sessions)
        {
            session.Revoke();
            await _sessionRepository.UpdateAsync(session, cancellationToken);
        }

        if (sessions.Count > 0)
        {
            await _sessionRepository.SaveChangesAsync(cancellationToken);
        }

        return sessions.Count;
    }

    /// <summary>
    /// Gets all active sessions for a user.
    /// Useful for session management UI.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active sessions.</returns>
    public async Task<IReadOnlyList<UserSession>> GetActiveSessionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return [];
        }

        return await _sessionRepository.GetActiveSessionsByUserIdAsync(userId, cancellationToken);
    }

    /// <summary>
    /// Cleans up expired and old revoked sessions.
    /// Should be called periodically by a background service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of sessions cleaned up.</returns>
    public async Task<int> CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        // Keep expired/revoked sessions for 7 days for auditing purposes
        return await _sessionRepository.CleanupExpiredSessionsAsync(TimeSpan.FromDays(7), cancellationToken);
    }
}
