using SD.Project.Domain.ValueObjects;

namespace SD.Project.Domain.Services;

/// <summary>
/// Represents the result of commission calculation for a seller.
/// This is used internally for financial settlements and is not visible to buyers.
/// </summary>
public sealed record SellerCommission(
    /// <summary>
    /// The store ID.
    /// </summary>
    Guid StoreId,

    /// <summary>
    /// The order subtotal from this seller (items only, excluding shipping).
    /// </summary>
    Money OrderSubtotal,

    /// <summary>
    /// The commission amount owed to the platform.
    /// </summary>
    Money CommissionAmount,

    /// <summary>
    /// The payout amount for the seller after commission deduction.
    /// </summary>
    Money SellerPayout,

    /// <summary>
    /// The commission rate applied (as a percentage, e.g., 10 for 10%).
    /// </summary>
    decimal CommissionRate);

/// <summary>
/// Domain service for calculating platform commissions from seller sales.
/// Commission calculations are internal and not visible to buyers.
/// This service ensures consistency with the central payments/settlements model.
/// </summary>
public sealed class CommissionCalculator
{
    /// <summary>
    /// Default platform commission rate (percentage).
    /// This default can be overridden per-calculation via the commissionRate parameter.
    /// For different rate tiers per seller, pass the appropriate rate when calling the calculation methods.
    /// </summary>
    public const decimal DefaultCommissionRate = 10m;

    /// <summary>
    /// Calculates the commission for a seller based on their order subtotal.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="orderSubtotal">The order subtotal from items (excluding shipping).</param>
    /// <param name="currency">The currency code.</param>
    /// <param name="commissionRate">Optional custom commission rate. Uses default if not specified.</param>
    /// <returns>The calculated commission breakdown.</returns>
    public SellerCommission CalculateCommission(
        Guid storeId,
        decimal orderSubtotal,
        string currency,
        decimal? commissionRate = null)
    {
        var rate = commissionRate ?? DefaultCommissionRate;

        if (rate < 0 || rate > 100)
        {
            throw new ArgumentException("Commission rate must be between 0 and 100.", nameof(commissionRate));
        }

        var commissionAmount = Math.Round(orderSubtotal * (rate / 100m), 2, MidpointRounding.ToEven);
        var sellerPayout = orderSubtotal - commissionAmount;

        return new SellerCommission(
            storeId,
            new Money(orderSubtotal, currency),
            new Money(commissionAmount, currency),
            new Money(sellerPayout, currency),
            rate);
    }

    /// <summary>
    /// Calculates commissions for multiple sellers.
    /// </summary>
    /// <param name="sellerSubtotals">Dictionary of store ID to order subtotal.</param>
    /// <param name="currency">The currency code.</param>
    /// <param name="commissionRate">Optional custom commission rate. Uses default if not specified.</param>
    /// <returns>Commission calculations for each seller.</returns>
    public IReadOnlyDictionary<Guid, SellerCommission> CalculateCommissions(
        IReadOnlyDictionary<Guid, decimal> sellerSubtotals,
        string currency,
        decimal? commissionRate = null)
    {
        return sellerSubtotals.ToDictionary(
            kvp => kvp.Key,
            kvp => CalculateCommission(kvp.Key, kvp.Value, currency, commissionRate));
    }
}
