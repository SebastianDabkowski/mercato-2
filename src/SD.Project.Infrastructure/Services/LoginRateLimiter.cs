using System.Collections.Concurrent;
using SD.Project.Application.Interfaces;

namespace SD.Project.Infrastructure.Services;

/// <summary>
/// In-memory rate limiter for login attempts.
/// Uses a sliding window approach to track failed attempts.
/// </summary>
public sealed class LoginRateLimiter : ILoginRateLimiter
{
    private readonly ConcurrentDictionary<string, LoginAttemptInfo> _attempts = new();
    private readonly int _maxAttempts;
    private readonly TimeSpan _windowDuration;

    public LoginRateLimiter()
        : this(maxAttempts: 5, windowDuration: TimeSpan.FromMinutes(15))
    {
    }

    public LoginRateLimiter(int maxAttempts, TimeSpan windowDuration)
    {
        _maxAttempts = maxAttempts;
        _windowDuration = windowDuration;
    }

    public bool IsRateLimited(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            return false;
        }

        var normalizedId = identifier.ToLowerInvariant();

        if (!_attempts.TryGetValue(normalizedId, out var info))
        {
            return false;
        }

        // Check if window has expired
        if (DateTime.UtcNow - info.WindowStart > _windowDuration)
        {
            // Remove expired entry
            _attempts.TryRemove(normalizedId, out _);
            return false;
        }

        return info.FailedAttempts >= _maxAttempts;
    }

    public void RecordFailedAttempt(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            return;
        }

        var normalizedId = identifier.ToLowerInvariant();
        var now = DateTime.UtcNow;

        _attempts.AddOrUpdate(
            normalizedId,
            _ => new LoginAttemptInfo { WindowStart = now, FailedAttempts = 1 },
            (_, existing) =>
            {
                // If window expired, start a new one
                if (now - existing.WindowStart > _windowDuration)
                {
                    return new LoginAttemptInfo { WindowStart = now, FailedAttempts = 1 };
                }

                existing.FailedAttempts++;
                return existing;
            });
    }

    public void ResetAttempts(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            return;
        }

        var normalizedId = identifier.ToLowerInvariant();
        _attempts.TryRemove(normalizedId, out _);
    }

    private sealed class LoginAttemptInfo
    {
        public DateTime WindowStart { get; set; }
        public int FailedAttempts { get; set; }
    }
}
