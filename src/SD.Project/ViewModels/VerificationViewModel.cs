namespace SD.Project.ViewModels;

/// <summary>
/// View model for the verification step of the onboarding wizard.
/// </summary>
public sealed class VerificationViewModel
{
    public string BusinessName { get; set; } = string.Empty;
    public string BusinessRegistrationNumber { get; set; } = string.Empty;
    public string TaxIdentificationNumber { get; set; } = string.Empty;
    public string BusinessAddress { get; set; } = string.Empty;
}
