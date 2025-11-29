namespace SD.Project.Domain.ValueObjects;

/// <summary>
/// Represents the result of validating a promo code.
/// </summary>
public sealed record ApplyPromoResult
{
    /// <summary>
    /// Whether the promo code was successfully validated and can be applied.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// The calculated discount amount (0 if not valid).
    /// </summary>
    public decimal DiscountAmount { get; }

    /// <summary>
    /// The promo code ID if valid.
    /// </summary>
    public Guid? PromoCodeId { get; }

    /// <summary>
    /// The promo code string.
    /// </summary>
    public string? PromoCode { get; }

    /// <summary>
    /// Description of the discount for display purposes.
    /// </summary>
    public string? DiscountDescription { get; }

    private ApplyPromoResult(bool isValid, string? errorMessage, decimal discountAmount, Guid? promoCodeId, string? promoCode, string? discountDescription)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
        DiscountAmount = discountAmount;
        PromoCodeId = promoCodeId;
        PromoCode = promoCode;
        DiscountDescription = discountDescription;
    }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ApplyPromoResult Success(Guid promoCodeId, string promoCode, decimal discountAmount, string discountDescription)
        => new(true, null, discountAmount, promoCodeId, promoCode, discountDescription);

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static ApplyPromoResult Failed(string errorMessage)
        => new(false, errorMessage, 0m, null, null, null);

    /// <summary>
    /// Promo code not found.
    /// </summary>
    public static ApplyPromoResult NotFound()
        => Failed("Promo code not found.");

    /// <summary>
    /// Promo code has expired.
    /// </summary>
    public static ApplyPromoResult Expired()
        => Failed("This promo code has expired.");

    /// <summary>
    /// Promo code is not yet active.
    /// </summary>
    public static ApplyPromoResult NotYetActive()
        => Failed("This promo code is not yet active.");

    /// <summary>
    /// Promo code has been deactivated.
    /// </summary>
    public static ApplyPromoResult Inactive()
        => Failed("This promo code is no longer active.");

    /// <summary>
    /// Promo code has reached its maximum usage limit.
    /// </summary>
    public static ApplyPromoResult MaxUsageReached()
        => Failed("This promo code has reached its maximum usage limit.");

    /// <summary>
    /// User has reached their usage limit for this promo code.
    /// </summary>
    public static ApplyPromoResult UserLimitReached()
        => Failed("You have already used this promo code the maximum number of times.");

    /// <summary>
    /// Order does not meet minimum amount requirement.
    /// </summary>
    public static ApplyPromoResult MinimumNotMet(decimal minimumAmount, string currency)
        => Failed($"Minimum order amount of {currency} {minimumAmount:N2} required for this promo code.");

    /// <summary>
    /// Promo code is not applicable to the cart items.
    /// </summary>
    public static ApplyPromoResult NotApplicable(string reason)
        => Failed($"This promo code is not applicable: {reason}");

    /// <summary>
    /// Currency mismatch between promo code and cart.
    /// </summary>
    public static ApplyPromoResult CurrencyMismatch()
        => Failed("This promo code is not valid for your cart's currency.");
}
