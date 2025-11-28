using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for email verification token persistence operations.
/// </summary>
public interface IEmailVerificationTokenRepository
{
    /// <summary>
    /// Gets a verification token by its unique token string.
    /// </summary>
    Task<EmailVerificationToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest valid (unused and not expired) verification token for a user.
    /// </summary>
    Task<EmailVerificationToken?> GetLatestValidTokenForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new verification token to the repository.
    /// </summary>
    Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves changes made to entities.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
