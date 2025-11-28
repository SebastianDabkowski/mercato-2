namespace SD.Project.ViewModels;

/// <summary>
/// View model for the payout step of the onboarding wizard.
/// </summary>
public sealed class PayoutViewModel
{
    public string BankAccountHolder { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankSwiftCode { get; set; } = string.Empty;
}
