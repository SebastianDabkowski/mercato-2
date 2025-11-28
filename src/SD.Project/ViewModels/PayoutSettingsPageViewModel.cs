using System.ComponentModel.DataAnnotations;

namespace SD.Project.ViewModels;

/// <summary>
/// View model for the payout settings page.
/// </summary>
public sealed class PayoutSettingsPageViewModel
{
    // Bank Transfer fields
    [Display(Name = "Account Holder Name")]
    public string BankAccountHolder { get; set; } = string.Empty;

    [Display(Name = "Bank Account Number / IBAN")]
    public string BankAccountNumber { get; set; } = string.Empty;

    [Display(Name = "Bank Name")]
    public string BankName { get; set; } = string.Empty;

    [Display(Name = "SWIFT/BIC Code")]
    public string BankSwiftCode { get; set; } = string.Empty;

    [Display(Name = "Bank Country")]
    public string? BankCountry { get; set; }

    // SEPA fields
    [Display(Name = "IBAN")]
    public string SepaIban { get; set; } = string.Empty;

    [Display(Name = "BIC")]
    public string SepaBic { get; set; } = string.Empty;
}
