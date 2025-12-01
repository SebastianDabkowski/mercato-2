using System.Diagnostics;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using SD.Project.Application.Interfaces;
using SD.Project.Domain.Entities;

namespace SD.Project.Infrastructure.Services;

/// <summary>
/// Implementation of integration health checker that tests connectivity to external services.
/// </summary>
public sealed class IntegrationHealthChecker : IIntegrationHealthChecker
{
    private readonly ILogger<IntegrationHealthChecker> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    public IntegrationHealthChecker(ILogger<IntegrationHealthChecker> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<(bool Success, string Message, int? ResponseTimeMs)> TestConnectionAsync(
        IntegrationType integrationType,
        string endpoint,
        string? apiKey,
        string? merchantId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return (false, "Endpoint is required.", null);
        }

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            return (false, "Invalid endpoint URL.", null);
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var client = _httpClientFactory.CreateClient("IntegrationHealthCheck");
            client.Timeout = DefaultTimeout;

            // Create the request
            var request = new HttpRequestMessage(HttpMethod.Get, uri);

            // Add authorization header if API key is provided
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                // Different integration types may use different authentication methods
                switch (integrationType)
                {
                    case IntegrationType.Payment:
                        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
                        break;
                    case IntegrationType.Shipping:
                        request.Headers.TryAddWithoutValidation("X-Api-Key", apiKey);
                        break;
                    default:
                        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
                        break;
                }
            }

            // Add merchant ID header if provided
            if (!string.IsNullOrWhiteSpace(merchantId))
            {
                request.Headers.TryAddWithoutValidation("X-Merchant-Id", merchantId);
            }

            var response = await client.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            var responseTimeMs = (int)stopwatch.ElapsedMilliseconds;

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Integration health check succeeded for {IntegrationType} at {Endpoint} in {ResponseTimeMs}ms",
                    integrationType, endpoint, responseTimeMs);

                return (true, $"Connection successful. Response time: {responseTimeMs}ms", responseTimeMs);
            }
            else
            {
                var statusCode = (int)response.StatusCode;
                var reasonPhrase = response.ReasonPhrase ?? "Unknown";

                _logger.LogWarning(
                    "Integration health check failed for {IntegrationType} at {Endpoint}: {StatusCode} {ReasonPhrase}",
                    integrationType, endpoint, statusCode, reasonPhrase);

                return (false, $"Connection failed with status {statusCode}: {reasonPhrase}", responseTimeMs);
            }
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Integration health check failed for {IntegrationType} at {Endpoint}: {ErrorMessage}",
                integrationType, endpoint, ex.Message);

            return (false, $"Connection failed: {ex.Message}", null);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Integration health check timed out for {IntegrationType} at {Endpoint}",
                integrationType, endpoint);

            return (false, "Connection timed out. The service may be unavailable or slow to respond.", null);
        }
        catch (TaskCanceledException)
        {
            stopwatch.Stop();
            return (false, "Request was cancelled.", null);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Unexpected error during integration health check for {IntegrationType} at {Endpoint}",
                integrationType, endpoint);

            return (false, $"Unexpected error: {ex.Message}", null);
        }
    }
}
