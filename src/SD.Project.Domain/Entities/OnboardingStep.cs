namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the steps in the seller onboarding wizard.
/// </summary>
public enum OnboardingStep
{
    /// <summary>
    /// Step 1: Store profile basics (store name, description, etc.)
    /// </summary>
    StoreProfile = 1,

    /// <summary>
    /// Step 2: Verification data (business info, documents)
    /// </summary>
    Verification = 2,

    /// <summary>
    /// Step 3: Payout settings (bank account, payment method)
    /// </summary>
    Payout = 3,

    /// <summary>
    /// All steps completed and wizard submitted.
    /// </summary>
    Completed = 4
}
