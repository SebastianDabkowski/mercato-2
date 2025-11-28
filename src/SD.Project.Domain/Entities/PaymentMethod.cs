namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a payment method available in the marketplace.
/// Payment methods are configured centrally by the platform.
/// </summary>
public class PaymentMethod
{
    public Guid Id { get; private set; }

    /// <summary>
    /// Display name of the payment method (e.g., "Credit Card", "PayPal").
    /// </summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// Short description of the payment method.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Type of payment method.
    /// </summary>
    public PaymentMethodType Type { get; private set; }

    /// <summary>
    /// Payment provider identifier (e.g., "stripe", "paypal").
    /// </summary>
    public string Provider { get; private set; } = default!;

    /// <summary>
    /// Icon class or URL for the payment method.
    /// </summary>
    public string? IconClass { get; private set; }

    /// <summary>
    /// Display order for sorting payment methods.
    /// </summary>
    public int DisplayOrder { get; private set; }

    /// <summary>
    /// Whether this payment method is currently active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Whether this is the default payment method.
    /// </summary>
    public bool IsDefault { get; private set; }

    /// <summary>
    /// Processing fee percentage (e.g., 2.9 for 2.9%).
    /// </summary>
    public decimal? FeePercentage { get; private set; }

    /// <summary>
    /// Fixed processing fee amount.
    /// </summary>
    public decimal? FeeFixed { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private PaymentMethod()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new payment method.
    /// </summary>
    public PaymentMethod(
        string name,
        PaymentMethodType type,
        string provider,
        string? description = null,
        string? iconClass = null,
        int displayOrder = 0,
        decimal? feePercentage = null,
        decimal? feeFixed = null,
        bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Payment method name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new ArgumentException("Provider is required.", nameof(provider));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = description?.Trim();
        Type = type;
        Provider = provider.ToLowerInvariant();
        IconClass = iconClass;
        DisplayOrder = displayOrder;
        IsActive = true;
        IsDefault = isDefault;
        FeePercentage = feePercentage;
        FeeFixed = feeFixed;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the payment method details.
    /// </summary>
    public void Update(
        string name,
        string? description,
        string? iconClass,
        int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Payment method name is required.", nameof(name));
        }

        Name = name.Trim();
        Description = description?.Trim();
        IconClass = iconClass;
        DisplayOrder = displayOrder;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the fee structure.
    /// </summary>
    public void UpdateFees(decimal? feePercentage, decimal? feeFixed)
    {
        FeePercentage = feePercentage;
        FeeFixed = feeFixed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the payment method.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the payment method.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        IsDefault = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets this as the default payment method.
    /// </summary>
    public void SetAsDefault()
    {
        IsDefault = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes the default status.
    /// </summary>
    public void RemoveDefault()
    {
        IsDefault = false;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Type of payment method.
/// </summary>
public enum PaymentMethodType
{
    /// <summary>Credit or debit card payment.</summary>
    Card,
    /// <summary>Bank transfer or direct debit.</summary>
    BankTransfer,
    /// <summary>Digital wallet (PayPal, Apple Pay, etc.).</summary>
    DigitalWallet,
    /// <summary>Buy now pay later services.</summary>
    BuyNowPayLater,
    /// <summary>Cash on delivery.</summary>
    CashOnDelivery
}
