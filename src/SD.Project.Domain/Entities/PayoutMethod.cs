namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the available payout method types.
/// </summary>
public enum PayoutMethod
{
    /// <summary>
    /// No payout method has been configured.
    /// </summary>
    None = 0,

    /// <summary>
    /// Bank transfer via SWIFT/BIC.
    /// </summary>
    BankTransfer = 1,

    /// <summary>
    /// SEPA bank transfer (European region).
    /// </summary>
    Sepa = 2
}
