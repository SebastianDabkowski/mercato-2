using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Repository contract for internal user invitation operations.
/// </summary>
public interface IInternalUserInvitationRepository
{
    /// <summary>
    /// Gets an invitation by its ID.
    /// </summary>
    Task<InternalUserInvitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an invitation by its token.
    /// </summary>
    Task<InternalUserInvitation?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest valid invitation for an internal user.
    /// </summary>
    Task<InternalUserInvitation?> GetLatestByInternalUserIdAsync(Guid internalUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new invitation.
    /// </summary>
    Task AddAsync(InternalUserInvitation invitation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists changes to the underlying store.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
