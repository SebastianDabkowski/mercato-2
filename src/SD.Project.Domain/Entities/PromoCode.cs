namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the scope of a promo code - who issued it and where it applies.
/// </summary>
public enum PromoCodeScope
{
    /// <summary>Platform-wide promo code issued by Mercato.</summary>
    Platform,
    /// <summary>Seller-specific promo code issued by a seller.</summary>
    Seller
}

/// <summary>
/// Represents the discount type of a promo code.
/// </summary>
public enum PromoDiscountType
{
    /// <summary>Fixed amount discount.</summary>
    FixedAmount,
    /// <summary>Percentage discount.</summary>
    Percentage
}

/// <summary>
/// Represents a promotional code that can be applied during checkout.
/// Promo codes may be issued by Mercato (platform-wide) or by individual sellers.
/// </summary>
public class PromoCode
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The unique promo code string that users enter.
    /// </summary>
    public string Code { get; private set; } = default!;

    /// <summary>
    /// Optional description of the promo code for admin/seller use.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// The scope of the promo code (Platform or Seller).
    /// </summary>
    public PromoCodeScope Scope { get; private set; }

    /// <summary>
    /// The store ID if this is a seller-specific promo code.
    /// Null for platform-wide promo codes.
    /// </summary>
    public Guid? StoreId { get; private set; }

    /// <summary>
    /// The type of discount (Fixed or Percentage).
    /// </summary>
    public PromoDiscountType DiscountType { get; private set; }

    /// <summary>
    /// The discount value. For percentage discounts, this is a value between 0-100.
    /// For fixed amount discounts, this is the currency amount.
    /// </summary>
    public decimal DiscountValue { get; private set; }

    /// <summary>
    /// The currency code for fixed amount discounts.
    /// </summary>
    public string Currency { get; private set; } = default!;

    /// <summary>
    /// Minimum order subtotal required to apply this promo code.
    /// </summary>
    public decimal? MinimumOrderAmount { get; private set; }

    /// <summary>
    /// Maximum discount amount for percentage discounts (optional cap).
    /// </summary>
    public decimal? MaximumDiscountAmount { get; private set; }

    /// <summary>
    /// The date and time when the promo code becomes valid.
    /// </summary>
    public DateTime ValidFrom { get; private set; }

    /// <summary>
    /// The date and time when the promo code expires.
    /// </summary>
    public DateTime ValidTo { get; private set; }

    /// <summary>
    /// Maximum number of times this promo code can be used. Null for unlimited.
    /// </summary>
    public int? MaxUsageCount { get; private set; }

    /// <summary>
    /// Current usage count.
    /// </summary>
    public int UsageCount { get; private set; }

    /// <summary>
    /// Maximum number of times a single user can use this promo code. Null for unlimited.
    /// </summary>
    public int? MaxUsagePerUser { get; private set; }

    /// <summary>
    /// Whether the promo code is currently active.
    /// </summary>
    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private PromoCode()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new platform-wide promo code.
    /// </summary>
    public static PromoCode CreatePlatformPromo(
        string code,
        string? description,
        PromoDiscountType discountType,
        decimal discountValue,
        string currency,
        DateTime validFrom,
        DateTime validTo,
        decimal? minimumOrderAmount = null,
        decimal? maximumDiscountAmount = null,
        int? maxUsageCount = null,
        int? maxUsagePerUser = null)
    {
        return new PromoCode(
            code,
            description,
            PromoCodeScope.Platform,
            storeId: null,
            discountType,
            discountValue,
            currency,
            validFrom,
            validTo,
            minimumOrderAmount,
            maximumDiscountAmount,
            maxUsageCount,
            maxUsagePerUser);
    }

    /// <summary>
    /// Creates a new seller-specific promo code.
    /// </summary>
    public static PromoCode CreateSellerPromo(
        string code,
        string? description,
        Guid storeId,
        PromoDiscountType discountType,
        decimal discountValue,
        string currency,
        DateTime validFrom,
        DateTime validTo,
        decimal? minimumOrderAmount = null,
        decimal? maximumDiscountAmount = null,
        int? maxUsageCount = null,
        int? maxUsagePerUser = null)
    {
        if (storeId == Guid.Empty)
        {
            throw new ArgumentException("Store ID is required for seller promo codes.", nameof(storeId));
        }

        return new PromoCode(
            code,
            description,
            PromoCodeScope.Seller,
            storeId,
            discountType,
            discountValue,
            currency,
            validFrom,
            validTo,
            minimumOrderAmount,
            maximumDiscountAmount,
            maxUsageCount,
            maxUsagePerUser);
    }

    private PromoCode(
        string code,
        string? description,
        PromoCodeScope scope,
        Guid? storeId,
        PromoDiscountType discountType,
        decimal discountValue,
        string currency,
        DateTime validFrom,
        DateTime validTo,
        decimal? minimumOrderAmount,
        decimal? maximumDiscountAmount,
        int? maxUsageCount,
        int? maxUsagePerUser)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Promo code is required.", nameof(code));
        }

        if (discountValue <= 0)
        {
            throw new ArgumentException("Discount value must be greater than zero.", nameof(discountValue));
        }

        if (discountType == PromoDiscountType.Percentage && discountValue > 100)
        {
            throw new ArgumentException("Percentage discount cannot exceed 100.", nameof(discountValue));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        if (validTo <= validFrom)
        {
            throw new ArgumentException("Valid to date must be after valid from date.", nameof(validTo));
        }

        Id = Guid.NewGuid();
        Code = code.ToUpperInvariant().Trim();
        Description = description;
        Scope = scope;
        StoreId = storeId;
        DiscountType = discountType;
        DiscountValue = discountValue;
        Currency = currency.ToUpperInvariant();
        ValidFrom = validFrom;
        ValidTo = validTo;
        MinimumOrderAmount = minimumOrderAmount;
        MaximumDiscountAmount = maximumDiscountAmount;
        MaxUsageCount = maxUsageCount;
        MaxUsagePerUser = maxUsagePerUser;
        UsageCount = 0;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the promo code is currently valid (active, within date range, and not exhausted).
    /// </summary>
    public bool IsCurrentlyValid()
    {
        if (!IsActive)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        if (now < ValidFrom || now > ValidTo)
        {
            return false;
        }

        if (MaxUsageCount.HasValue && UsageCount >= MaxUsageCount.Value)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Calculates the discount amount for the given order subtotal.
    /// </summary>
    /// <param name="applicableSubtotal">The subtotal to which the discount applies.</param>
    /// <returns>The calculated discount amount.</returns>
    public decimal CalculateDiscount(decimal applicableSubtotal)
    {
        if (applicableSubtotal <= 0)
        {
            return 0m;
        }

        decimal discount;
        if (DiscountType == PromoDiscountType.Percentage)
        {
            discount = applicableSubtotal * (DiscountValue / 100m);

            // Apply maximum discount cap if specified
            if (MaximumDiscountAmount.HasValue && discount > MaximumDiscountAmount.Value)
            {
                discount = MaximumDiscountAmount.Value;
            }
        }
        else
        {
            // Fixed amount discount - don't exceed the subtotal
            discount = Math.Min(DiscountValue, applicableSubtotal);
        }

        return Math.Round(discount, 2);
    }

    /// <summary>
    /// Increments the usage count when the promo code is used.
    /// </summary>
    public void IncrementUsage()
    {
        UsageCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the promo code.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reactivates the promo code.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
