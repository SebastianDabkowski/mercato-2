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
/// Represents the result of commission calculation for a partial refund.
/// </summary>
public sealed record RefundCommission(
    /// <summary>
    /// The original commission amount before refund.
    /// </summary>
    Money OriginalCommissionAmount,

    /// <summary>
    /// The commission amount to be refunded (proportional to refund).
    /// </summary>
    Money RefundedCommissionAmount,

    /// <summary>
    /// The remaining commission amount after refund.
    /// </summary>
    Money RemainingCommissionAmount,

    /// <summary>
    /// The original commission rate that was applied.
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

        var commissionAmount = CalculateCommissionAmount(orderSubtotal, rate);
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

    /// <summary>
    /// Calculates the commission impact of a partial refund.
    /// Uses the original commission rate to maintain consistency with historical orders.
    /// </summary>
    /// <param name="originalAmount">The original order subtotal.</param>
    /// <param name="refundAmount">The amount being refunded.</param>
    /// <param name="originalCommissionRate">The commission rate that was applied at payment confirmation.</param>
    /// <param name="currency">The currency code.</param>
    /// <returns>The refund commission breakdown.</returns>
    public RefundCommission CalculateRefundCommission(
        decimal originalAmount,
        decimal refundAmount,
        decimal originalCommissionRate,
        string currency)
    {
        if (originalAmount <= 0)
        {
            throw new ArgumentException("Original amount must be greater than zero.", nameof(originalAmount));
        }

        if (refundAmount <= 0)
        {
            throw new ArgumentException("Refund amount must be greater than zero.", nameof(refundAmount));
        }

        if (refundAmount > originalAmount)
        {
            throw new ArgumentException("Refund amount cannot exceed original amount.", nameof(refundAmount));
        }

        if (originalCommissionRate < 0 || originalCommissionRate > 100)
        {
            throw new ArgumentException("Commission rate must be between 0 and 100.", nameof(originalCommissionRate));
        }

        var originalCommission = CalculateCommissionAmount(originalAmount, originalCommissionRate);
        var remainingAmount = originalAmount - refundAmount;
        var remainingCommission = CalculateCommissionAmount(remainingAmount, originalCommissionRate);
        var refundedCommission = originalCommission - remainingCommission;

        return new RefundCommission(
            new Money(originalCommission, currency),
            new Money(refundedCommission, currency),
            new Money(remainingCommission, currency),
            originalCommissionRate);
    }

    /// <summary>
    /// Calculates the commission amount with high precision.
    /// Uses banker's rounding (MidpointRounding.ToEven) for fairness.
    /// </summary>
    /// <param name="amount">The amount to calculate commission on.</param>
    /// <param name="rate">The commission rate as a percentage.</param>
    /// <returns>The commission amount rounded to 2 decimal places.</returns>
    private static decimal CalculateCommissionAmount(decimal amount, decimal rate)
    {
        // Use high precision for intermediate calculation, round only at the end
        return Math.Round(amount * (rate / 100m), 2, MidpointRounding.ToEven);
    }
}
