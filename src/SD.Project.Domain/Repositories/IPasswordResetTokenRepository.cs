using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for password reset token persistence operations.
/// </summary>
public interface IPasswordResetTokenRepository
{
    /// <summary>
    /// Gets a password reset token by its unique token string.
    /// </summary>
    Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all valid (unused and not expired) password reset tokens for a user.
    /// </summary>
    Task<IReadOnlyList<PasswordResetToken>> GetValidTokensForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new password reset token to the repository.
    /// </summary>
    Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves changes made to entities.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
