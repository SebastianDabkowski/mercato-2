namespace SD.Project.Application.Interfaces;

/// <summary>
/// Abstraction for rate limiting login attempts to mitigate brute-force attacks.
/// </summary>
public interface ILoginRateLimiter
{
    /// <summary>
    /// Checks if the provided identifier (email or IP) has exceeded the rate limit.
    /// </summary>
    bool IsRateLimited(string identifier);

    /// <summary>
    /// Records a failed login attempt for the given identifier.
    /// </summary>
    void RecordFailedAttempt(string identifier);

    /// <summary>
    /// Clears recorded attempts for the given identifier (e.g., after successful login).
    /// </summary>
    void ResetAttempts(string identifier);
}
