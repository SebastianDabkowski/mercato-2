using SD.Project.Domain.Entities;

namespace SD.Project.ViewModels;

/// <summary>
/// View model for the verification step of the onboarding wizard.
/// </summary>
public sealed class VerificationViewModel
{
    public SellerType SellerType { get; set; } = SellerType.NotSpecified;
    
    // Company verification fields
    public string BusinessName { get; set; } = string.Empty;
    public string BusinessRegistrationNumber { get; set; } = string.Empty;
    public string TaxIdentificationNumber { get; set; } = string.Empty;
    public string BusinessAddress { get; set; } = string.Empty;
    public string ContactPersonName { get; set; } = string.Empty;
    public string ContactPersonEmail { get; set; } = string.Empty;
    public string ContactPersonPhone { get; set; } = string.Empty;
    
    // Individual verification fields
    public string FullName { get; set; } = string.Empty;
    public string PersonalIdNumber { get; set; } = string.Empty;
    public string PersonalAddress { get; set; } = string.Empty;
    public string PersonalEmail { get; set; } = string.Empty;
    public string PersonalPhone { get; set; } = string.Empty;
}
