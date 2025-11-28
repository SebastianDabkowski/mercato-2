using Microsoft.Extensions.Logging;
using SD.Project.Application.Interfaces;
using SD.Project.Domain.Repositories;

namespace SD.Project.Infrastructure.Services;

/// <summary>
/// Service for detecting and alerting on unusual login activity.
/// </summary>
public sealed class SecurityAlertService : ISecurityAlertService
{
    private readonly ILoginEventRepository _loginEventRepository;
    private readonly ILogger<SecurityAlertService> _logger;

    // Configuration constants (could be moved to appsettings)
    private const int FailedLoginThreshold = 5;
    private const int FailedLoginWindowMinutes = 15;
    private const int NewLocationWindowDays = 30;

    public SecurityAlertService(
        ILoginEventRepository loginEventRepository,
        ILogger<SecurityAlertService> logger)
    {
        _loginEventRepository = loginEventRepository;
        _logger = logger;
    }

    public async Task<bool> AnalyzeLoginAsync(
        Guid userId,
        string email,
        bool isSuccess,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return false;
        }

        var alertTriggered = false;

        // Check for multiple failed login attempts
        if (!isSuccess)
        {
            var since = DateTime.UtcNow.AddMinutes(-FailedLoginWindowMinutes);
            var failedCount = await _loginEventRepository.CountFailedLoginsSinceAsync(userId, since, cancellationToken);

            if (failedCount >= FailedLoginThreshold)
            {
                await SendSecurityAlertAsync(
                    userId,
                    email,
                    SecurityAlertType.MultipleFailedLogins,
                    $"Detected {failedCount} failed login attempts in the last {FailedLoginWindowMinutes} minutes.",
                    cancellationToken);
                alertTriggered = true;
            }
        }

        // Check for login from new location (only for successful logins)
        if (isSuccess && !string.IsNullOrEmpty(ipAddress))
        {
            var since = DateTime.UtcNow.AddDays(-NewLocationWindowDays);
            var knownIpAddresses = await _loginEventRepository.GetDistinctIpAddressesAsync(userId, since, cancellationToken);

            // Normalize IP for comparison (handles IPv6 case variations)
            var normalizedIp = NormalizeIpAddress(ipAddress);
            var knownNormalizedIps = knownIpAddresses.Select(NormalizeIpAddress).ToList();

            if (knownNormalizedIps.Count > 0 && !knownNormalizedIps.Contains(normalizedIp))
            {
                await SendSecurityAlertAsync(
                    userId,
                    email,
                    SecurityAlertType.NewLocation,
                    $"Login detected from a new IP address: {MaskIpAddress(ipAddress)}",
                    cancellationToken);
                alertTriggered = true;
            }
        }

        return alertTriggered;
    }

    public Task SendSecurityAlertAsync(
        Guid userId,
        string email,
        SecurityAlertType alertType,
        string details,
        CancellationToken cancellationToken = default)
    {
        // TODO: Replace logging with real notification integration (email, push notification, etc.).
        _logger.LogWarning(
            "Security alert for user {UserId} ({Email}): {AlertType} - {Details}",
            userId, email, alertType, details);

        return Task.CompletedTask;
    }

    private static string NormalizeIpAddress(string ipAddress)
    {
        // Normalize IP address for comparison (handles IPv6 case variations)
        if (string.IsNullOrEmpty(ipAddress))
        {
            return string.Empty;
        }

        // Try to parse as IP address for proper normalization
        if (System.Net.IPAddress.TryParse(ipAddress, out var parsedIp))
        {
            return parsedIp.ToString().ToLowerInvariant();
        }

        // Fallback to lowercase if parsing fails
        return ipAddress.Trim().ToLowerInvariant();
    }

    private static string MaskIpAddress(string ipAddress)
    {
        // Partially mask IP address for privacy in logs/alerts
        if (string.IsNullOrEmpty(ipAddress))
        {
            return string.Empty;
        }

        // Try to parse as IP address for proper validation
        if (System.Net.IPAddress.TryParse(ipAddress, out var parsedIp))
        {
            if (parsedIp.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                // IPv4: show first two octets, mask last two
                var bytes = parsedIp.GetAddressBytes();
                return $"{bytes[0]}.{bytes[1]}.xxx.xxx";
            }

            if (parsedIp.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                // IPv6: show first segment, mask rest
                var fullAddress = parsedIp.ToString();
                var colonIndex = fullAddress.IndexOf(':');
                if (colonIndex > 0)
                {
                    return $"{fullAddress[..colonIndex]}:xxxx::xxxx";
                }
            }
        }

        // Fallback for invalid or unknown format
        return "[masked IP]";
    }
}
