namespace SD.Project.Application.Interfaces;

/// <summary>
/// Abstraction for notifying users about changes.
/// </summary>
public interface INotificationService
{
    Task SendProductCreatedAsync(Guid productId, CancellationToken cancellationToken = default);
}
