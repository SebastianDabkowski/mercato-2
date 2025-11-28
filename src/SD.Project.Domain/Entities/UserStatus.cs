namespace SD.Project.Domain.Entities;

/// <summary>
/// Defines the account verification status of a user.
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// The user's email has not yet been verified.
    /// </summary>
    Unverified = 0,

    /// <summary>
    /// The user's email has been verified and the account is active.
    /// </summary>
    Verified = 1,

    /// <summary>
    /// The user's account has been suspended by an administrator.
    /// </summary>
    Suspended = 2
}
