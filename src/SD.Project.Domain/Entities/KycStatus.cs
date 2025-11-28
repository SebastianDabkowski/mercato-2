namespace SD.Project.Domain.Entities;

/// <summary>
/// Defines the Know Your Customer (KYC) verification status for sellers.
/// </summary>
public enum KycStatus
{
    /// <summary>
    /// KYC verification has not been started.
    /// </summary>
    NotStarted = 0,

    /// <summary>
    /// KYC verification is in progress and awaiting review.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// KYC verification has been approved.
    /// </summary>
    Approved = 2,

    /// <summary>
    /// KYC verification has been rejected.
    /// </summary>
    Rejected = 3
}
