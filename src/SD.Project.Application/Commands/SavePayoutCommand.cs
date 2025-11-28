namespace SD.Project.Application.Commands;

/// <summary>
/// Command to save payout step data in the onboarding wizard.
/// </summary>
public sealed record SavePayoutCommand(
    Guid UserId,
    string BankAccountHolder,
    string BankAccountNumber,
    string BankName,
    string BankSwiftCode,
    bool CompleteStep);
