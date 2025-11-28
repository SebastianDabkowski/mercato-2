namespace SD.Project.ViewModels;

/// <summary>
/// View model for the store profile step of the onboarding wizard.
/// </summary>
public sealed class StoreProfileViewModel
{
    public string StoreName { get; set; } = string.Empty;
    public string StoreDescription { get; set; } = string.Empty;
    public string StoreAddress { get; set; } = string.Empty;
    public string StoreCity { get; set; } = string.Empty;
    public string StorePostalCode { get; set; } = string.Empty;
    public string StoreCountry { get; set; } = string.Empty;
}
