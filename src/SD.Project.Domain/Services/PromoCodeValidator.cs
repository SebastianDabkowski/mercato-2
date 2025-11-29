using SD.Project.Domain.Entities;
using SD.Project.Domain.ValueObjects;

namespace SD.Project.Domain.Services;

/// <summary>
/// Domain service for validating and applying promo codes to carts.
/// </summary>
public sealed class PromoCodeValidator
{
    /// <summary>
    /// Validates a promo code against cart data and user usage.
    /// </summary>
    /// <param name="promoCode">The promo code to validate.</param>
    /// <param name="cartSubtotal">The total subtotal of the cart.</param>
    /// <param name="cartCurrency">The cart currency.</param>
    /// <param name="storeSubtotals">Dictionary of store ID to subtotal for seller-specific promos.</param>
    /// <param name="userUsageCount">Number of times the user has used this promo code.</param>
    /// <returns>The validation result with discount amount if valid.</returns>
    public ApplyPromoResult Validate(
        PromoCode promoCode,
        decimal cartSubtotal,
        string cartCurrency,
        IReadOnlyDictionary<Guid, decimal>? storeSubtotals,
        int userUsageCount)
    {
        ArgumentNullException.ThrowIfNull(promoCode);

        // Check if promo code is active
        if (!promoCode.IsActive)
        {
            return ApplyPromoResult.Inactive();
        }

        // Check date validity
        var now = DateTime.UtcNow;
        if (now < promoCode.ValidFrom)
        {
            return ApplyPromoResult.NotYetActive();
        }

        if (now > promoCode.ValidTo)
        {
            return ApplyPromoResult.Expired();
        }

        // Check global usage limit
        if (promoCode.MaxUsageCount.HasValue && promoCode.UsageCount >= promoCode.MaxUsageCount.Value)
        {
            return ApplyPromoResult.MaxUsageReached();
        }

        // Check user usage limit
        if (promoCode.MaxUsagePerUser.HasValue && userUsageCount >= promoCode.MaxUsagePerUser.Value)
        {
            return ApplyPromoResult.UserLimitReached();
        }

        // Check currency match
        if (!string.Equals(promoCode.Currency, cartCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return ApplyPromoResult.CurrencyMismatch();
        }

        // Determine the applicable subtotal based on promo scope
        decimal applicableSubtotal;
        if (promoCode.Scope == PromoCodeScope.Seller && promoCode.StoreId.HasValue)
        {
            // For seller-specific promos, only apply to items from that store
            if (storeSubtotals is null || !storeSubtotals.TryGetValue(promoCode.StoreId.Value, out applicableSubtotal))
            {
                return ApplyPromoResult.NotApplicable("Your cart doesn't contain items from the seller offering this promo.");
            }
        }
        else
        {
            // Platform-wide promo applies to entire cart
            applicableSubtotal = cartSubtotal;
        }

        // Check minimum order amount
        if (promoCode.MinimumOrderAmount.HasValue && applicableSubtotal < promoCode.MinimumOrderAmount.Value)
        {
            return ApplyPromoResult.MinimumNotMet(promoCode.MinimumOrderAmount.Value, promoCode.Currency);
        }

        // Calculate the discount
        var discountAmount = promoCode.CalculateDiscount(applicableSubtotal);
        if (discountAmount <= 0)
        {
            return ApplyPromoResult.NotApplicable("No discount applicable for this order.");
        }

        // Build discount description
        var description = BuildDiscountDescription(promoCode, discountAmount);

        return ApplyPromoResult.Success(promoCode.Id, promoCode.Code, discountAmount, description);
    }

    private static string BuildDiscountDescription(PromoCode promoCode, decimal discountAmount)
    {
        if (promoCode.DiscountType == PromoDiscountType.Percentage)
        {
            var percentText = $"{promoCode.DiscountValue:0.##}% off";
            if (promoCode.Scope == PromoCodeScope.Seller)
            {
                return $"{percentText} on seller items (-{promoCode.Currency} {discountAmount:N2})";
            }
            return $"{percentText} (-{promoCode.Currency} {discountAmount:N2})";
        }
        else
        {
            return $"{promoCode.Currency} {discountAmount:N2} off";
        }
    }
}
