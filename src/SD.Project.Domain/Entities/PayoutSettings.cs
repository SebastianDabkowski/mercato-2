namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a seller's payout configuration for receiving funds from sales.
/// </summary>
public class PayoutSettings
{
    public Guid Id { get; private set; }
    public Guid SellerId { get; private set; }

    // Primary payout method
    public PayoutMethod DefaultPayoutMethod { get; private set; }

    // Bank account details
    public string? BankAccountHolder { get; private set; }
    public string? BankAccountNumber { get; private set; }
    public string? BankName { get; private set; }
    public string? BankSwiftCode { get; private set; }
    public string? BankCountry { get; private set; }

    // SEPA-specific fields
    public string? SepaIban { get; private set; }
    public string? SepaBic { get; private set; }

    // Configuration status
    public bool IsConfigured { get; private set; }
    public bool IsVerified { get; private set; }

    // Timestamps
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? VerifiedAt { get; private set; }

    private PayoutSettings()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates new payout settings for the specified seller.
    /// </summary>
    public PayoutSettings(Guid sellerId)
    {
        if (sellerId == Guid.Empty)
        {
            throw new ArgumentException("Seller ID is required.", nameof(sellerId));
        }

        Id = Guid.NewGuid();
        SellerId = sellerId;
        DefaultPayoutMethod = PayoutMethod.None;
        IsConfigured = false;
        IsVerified = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates bank transfer payout configuration.
    /// </summary>
    public void UpdateBankTransfer(
        string bankAccountHolder,
        string bankAccountNumber,
        string bankName,
        string bankSwiftCode,
        string? bankCountry)
    {
        BankAccountHolder = bankAccountHolder?.Trim();
        BankAccountNumber = bankAccountNumber?.Trim();
        BankName = bankName?.Trim();
        BankSwiftCode = bankSwiftCode?.Trim();
        BankCountry = bankCountry?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates SEPA payout configuration.
    /// </summary>
    public void UpdateSepa(string iban, string bic)
    {
        SepaIban = iban?.Trim();
        SepaBic = bic?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the default payout method.
    /// </summary>
    public void SetDefaultPayoutMethod(PayoutMethod method)
    {
        if (method != PayoutMethod.None)
        {
            var errors = method switch
            {
                PayoutMethod.BankTransfer => GetBankTransferErrors(),
                PayoutMethod.Sepa => GetSepaErrors(),
                _ => new List<string> { "Invalid payout method." }
            };

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Cannot set {method} as default: {string.Join(" ", errors)}");
            }
        }

        DefaultPayoutMethod = method;
        IsConfigured = method != PayoutMethod.None;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the payout settings as verified after admin or system review.
    /// </summary>
    public void Verify()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Payout settings must be configured before verification.");
        }

        IsVerified = true;
        VerifiedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Revokes verification when payout settings are changed.
    /// </summary>
    public void RevokeVerification()
    {
        if (IsVerified)
        {
            IsVerified = false;
            VerifiedAt = null;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Gets the list of validation errors for bank transfer configuration.
    /// </summary>
    public IReadOnlyList<string> GetBankTransferErrors()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(BankAccountHolder))
            errors.Add("Bank account holder name is required.");

        if (string.IsNullOrWhiteSpace(BankAccountNumber))
            errors.Add("Bank account number is required.");

        if (string.IsNullOrWhiteSpace(BankName))
            errors.Add("Bank name is required.");

        if (string.IsNullOrWhiteSpace(BankSwiftCode))
            errors.Add("Bank SWIFT/BIC code is required.");
        else if (BankSwiftCode.Length < 8 || BankSwiftCode.Length > 11)
            errors.Add("SWIFT/BIC code must be 8-11 characters.");

        return errors;
    }

    /// <summary>
    /// Gets the list of validation errors for SEPA configuration.
    /// </summary>
    public IReadOnlyList<string> GetSepaErrors()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(SepaIban))
            errors.Add("IBAN is required for SEPA transfers.");
        else if (SepaIban.Length < 15 || SepaIban.Length > 34)
            errors.Add("IBAN must be between 15 and 34 characters.");

        if (string.IsNullOrWhiteSpace(SepaBic))
            errors.Add("BIC is required for SEPA transfers.");
        else if (SepaBic.Length < 8 || SepaBic.Length > 11)
            errors.Add("BIC must be 8-11 characters.");

        return errors;
    }

    /// <summary>
    /// Gets the list of all validation errors for the current configuration.
    /// </summary>
    public IReadOnlyList<string> GetConfigurationErrors()
    {
        var errors = new List<string>();

        if (DefaultPayoutMethod == PayoutMethod.None)
        {
            errors.Add("Please configure at least one payout method and set it as default.");
            return errors;
        }

        var methodErrors = DefaultPayoutMethod switch
        {
            PayoutMethod.BankTransfer => GetBankTransferErrors(),
            PayoutMethod.Sepa => GetSepaErrors(),
            _ => new List<string>()
        };

        errors.AddRange(methodErrors);
        return errors;
    }

    /// <summary>
    /// Indicates whether bank transfer is available (has valid configuration).
    /// </summary>
    public bool IsBankTransferAvailable => GetBankTransferErrors().Count == 0;

    /// <summary>
    /// Indicates whether SEPA is available (has valid configuration).
    /// </summary>
    public bool IsSepaAvailable => GetSepaErrors().Count == 0;
}
