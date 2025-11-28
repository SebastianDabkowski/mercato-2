namespace SD.Project.Application.Commands;

/// <summary>
/// Command to save bank transfer payout configuration.
/// </summary>
public sealed record SaveBankTransferPayoutCommand(
    Guid SellerId,
    string BankAccountHolder,
    string BankAccountNumber,
    string BankName,
    string BankSwiftCode,
    string? BankCountry,
    bool SetAsDefault);
