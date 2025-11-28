namespace SD.Project.Application.Commands;

/// <summary>
/// Command to save SEPA payout configuration.
/// </summary>
public sealed record SaveSepaPayoutCommand(
    Guid SellerId,
    string Iban,
    string Bic,
    bool SetAsDefault);
