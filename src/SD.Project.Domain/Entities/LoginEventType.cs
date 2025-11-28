namespace SD.Project.Domain.Entities;

/// <summary>
/// Defines the type of login event for auditing purposes.
/// </summary>
public enum LoginEventType
{
    /// <summary>
    /// Standard password-based login attempt.
    /// </summary>
    Password = 0,

    /// <summary>
    /// Social/external provider login (Google, Facebook, etc.).
    /// </summary>
    Social = 1,

    /// <summary>
    /// Two-factor authentication verification step.
    /// </summary>
    TwoFactor = 2,

    /// <summary>
    /// Login using a 2FA recovery code.
    /// </summary>
    RecoveryCode = 3,

    /// <summary>
    /// Session token refresh/validation.
    /// </summary>
    SessionRefresh = 4,

    /// <summary>
    /// Logout event.
    /// </summary>
    Logout = 5
}
