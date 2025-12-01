namespace SD.Project.Domain.Entities;

/// <summary>
/// Defines the reason for blocking a user account.
/// </summary>
public enum BlockReason
{
    /// <summary>
    /// Account blocked due to fraudulent activity.
    /// </summary>
    Fraud = 0,

    /// <summary>
    /// Account blocked due to spam activity.
    /// </summary>
    Spam = 1,

    /// <summary>
    /// Account blocked due to policy violation.
    /// </summary>
    PolicyViolation = 2,

    /// <summary>
    /// Account blocked for other reasons.
    /// </summary>
    Other = 3
}
