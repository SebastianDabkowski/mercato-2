using SD.Project.Domain.Entities;

namespace SD.Project.Application.Interfaces;

/// <summary>
/// Service interface for testing integration connections and performing health checks.
/// </summary>
public interface IIntegrationHealthChecker
{
    /// <summary>
    /// Tests the connection to an integration endpoint.
    /// </summary>
    /// <param name="integrationType">The type of integration to test.</param>
    /// <param name="endpoint">The API endpoint URL.</param>
    /// <param name="apiKey">Optional API key for authentication.</param>
    /// <param name="merchantId">Optional merchant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple of (success, message, responseTimeMs).</returns>
    Task<(bool Success, string Message, int? ResponseTimeMs)> TestConnectionAsync(
        IntegrationType integrationType,
        string endpoint,
        string? apiKey,
        string? merchantId,
        CancellationToken cancellationToken = default);
}
