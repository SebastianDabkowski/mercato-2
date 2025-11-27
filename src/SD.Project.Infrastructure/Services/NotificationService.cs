using Microsoft.Extensions.Logging;
using SD.Project.Application.Interfaces;

namespace SD.Project.Infrastructure.Services;

/// <summary>
/// Logs notification intents until real channel is available.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendProductCreatedAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        // TODO: Replace logging with integration (email, message bus, etc.).
        _logger.LogInformation("Product {ProductId} created", productId);
        return Task.CompletedTask;
    }
}
