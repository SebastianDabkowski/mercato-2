namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the overall status of a seller's onboarding process.
/// </summary>
public enum OnboardingStatus
{
    /// <summary>
    /// Onboarding is in progress (not all steps completed).
    /// </summary>
    InProgress = 0,

    /// <summary>
    /// All steps completed and submitted, pending verification.
    /// </summary>
    PendingVerification = 1,

    /// <summary>
    /// Seller account is active and verified.
    /// </summary>
    Verified = 2,

    /// <summary>
    /// Onboarding was rejected during verification.
    /// </summary>
    Rejected = 3
}
