namespace SD.Project.Application.Interfaces;

/// <summary>
/// Service for detecting and alerting on unusual login activity.
/// </summary>
public interface ISecurityAlertService
{
    /// <summary>
    /// Analyzes a login event and triggers alerts if unusual activity is detected.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="email">The email address.</param>
    /// <param name="isSuccess">Whether the login was successful.</param>
    /// <param name="ipAddress">The IP address of the login attempt.</param>
    /// <param name="userAgent">The user agent of the login attempt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if an alert was triggered, false otherwise.</returns>
    Task<bool> AnalyzeLoginAsync(
        Guid userId,
        string email,
        bool isSuccess,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a security alert to the user about suspicious activity.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="email">The email address to send the alert to.</param>
    /// <param name="alertType">The type of security alert.</param>
    /// <param name="details">Additional details about the alert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendSecurityAlertAsync(
        Guid userId,
        string email,
        SecurityAlertType alertType,
        string details,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Types of security alerts that can be triggered.
/// </summary>
public enum SecurityAlertType
{
    /// <summary>
    /// Multiple failed login attempts detected.
    /// </summary>
    MultipleFailedLogins = 0,

    /// <summary>
    /// Login from a new IP address or location.
    /// </summary>
    NewLocation = 1,

    /// <summary>
    /// Login from a new device or browser.
    /// </summary>
    NewDevice = 2,

    /// <summary>
    /// Two-factor authentication was enabled.
    /// </summary>
    TwoFactorEnabled = 3,

    /// <summary>
    /// Two-factor authentication was disabled.
    /// </summary>
    TwoFactorDisabled = 4,

    /// <summary>
    /// Password was changed.
    /// </summary>
    PasswordChanged = 5,

    /// <summary>
    /// Recovery code was used for login.
    /// </summary>
    RecoveryCodeUsed = 6
}
