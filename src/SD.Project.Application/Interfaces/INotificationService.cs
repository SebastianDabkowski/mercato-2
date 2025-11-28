namespace SD.Project.Application.Interfaces;

/// <summary>
/// Abstraction for notifying users about changes.
/// </summary>
public interface INotificationService
{
    Task SendProductCreatedAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a verification email to a newly registered user with a verification link.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="email">The email address to send to.</param>
    /// <param name="verificationToken">The unique verification token to include in the link.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendEmailVerificationAsync(Guid userId, string email, string verificationToken, CancellationToken cancellationToken = default);
}
