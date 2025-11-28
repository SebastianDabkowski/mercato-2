namespace SD.Project.Application.Interfaces;

/// <summary>
/// Abstraction for notifying users about changes.
/// </summary>
public interface INotificationService
{
    Task SendProductCreatedAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a verification email to a newly registered user.
    /// </summary>
    Task SendEmailVerificationAsync(Guid userId, string email, CancellationToken cancellationToken = default);
}
