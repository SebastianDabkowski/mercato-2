using SD.Project.Domain.Entities;
using SD.Project.Domain.ValueObjects;

namespace SD.Project.Domain.Services;

/// <summary>
/// Represents the calculated totals for a cart.
/// </summary>
public sealed record CartTotals(
    /// <summary>
    /// Sum of all item prices (quantity × unit price) across all sellers.
    /// </summary>
    Money ItemSubtotal,

    /// <summary>
    /// Shipping costs grouped by store.
    /// </summary>
    IReadOnlyDictionary<Guid, Money> ShippingByStore,

    /// <summary>
    /// Total shipping cost across all sellers.
    /// </summary>
    Money TotalShipping,

    /// <summary>
    /// Total amount payable (item subtotal + total shipping).
    /// </summary>
    Money TotalAmount);

/// <summary>
/// Represents the calculated totals for items from a single seller.
/// </summary>
public sealed record SellerCartTotals(
    /// <summary>
    /// The store ID.
    /// </summary>
    Guid StoreId,

    /// <summary>
    /// Sum of item prices for this seller.
    /// </summary>
    Money Subtotal,

    /// <summary>
    /// Shipping cost for this seller.
    /// </summary>
    Money Shipping,

    /// <summary>
    /// Total for this seller (subtotal + shipping).
    /// </summary>
    Money Total,

    /// <summary>
    /// Total item count for this seller.
    /// </summary>
    int ItemCount);

/// <summary>
/// Domain service for calculating cart totals including shipping.
/// </summary>
public sealed class CartTotalsCalculator
{
    /// <summary>
    /// Default shipping cost when no shipping rule is configured for a store.
    /// </summary>
    private const decimal DefaultBaseCost = 0m;
    private const decimal DefaultCostPerItem = 0m;

    /// <summary>
    /// Calculates totals for items from a single seller.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <param name="itemSubtotal">The subtotal for items from this seller.</param>
    /// <param name="itemCount">Total item count from this seller.</param>
    /// <param name="currency">The currency code.</param>
    /// <param name="shippingRule">The shipping rule for this store, or null for default.</param>
    /// <returns>The calculated totals for this seller.</returns>
    public SellerCartTotals CalculateSellerTotals(
        Guid storeId,
        decimal itemSubtotal,
        int itemCount,
        string currency,
        ShippingRule? shippingRule)
    {
        var shippingCost = CalculateShippingForStore(itemSubtotal, itemCount, shippingRule);
        var subtotal = new Money(itemSubtotal, currency);
        var shipping = new Money(shippingCost, currency);
        var total = new Money(itemSubtotal + shippingCost, currency);

        return new SellerCartTotals(storeId, subtotal, shipping, total, itemCount);
    }

    /// <summary>
    /// Calculates overall cart totals from seller totals.
    /// </summary>
    /// <param name="sellerTotals">The calculated totals for each seller.</param>
    /// <param name="currency">The currency code.</param>
    /// <returns>The aggregated cart totals.</returns>
    public CartTotals CalculateCartTotals(
        IEnumerable<SellerCartTotals> sellerTotals,
        string currency)
    {
        var sellerList = sellerTotals.ToList();

        var itemSubtotal = sellerList.Sum(s => s.Subtotal.Amount);
        var totalShipping = sellerList.Sum(s => s.Shipping.Amount);
        var totalAmount = itemSubtotal + totalShipping;

        var shippingByStore = sellerList.ToDictionary(
            s => s.StoreId,
            s => s.Shipping);

        return new CartTotals(
            new Money(itemSubtotal, currency),
            shippingByStore,
            new Money(totalShipping, currency),
            new Money(totalAmount, currency));
    }

    /// <summary>
    /// Calculates shipping cost for a single store.
    /// </summary>
    private static decimal CalculateShippingForStore(
        decimal subtotal,
        int itemCount,
        ShippingRule? shippingRule)
    {
        if (shippingRule is null)
        {
            // Default shipping: free (can be configured differently in the future)
            return DefaultBaseCost + (DefaultCostPerItem * itemCount);
        }

        return shippingRule.CalculateShippingCost(subtotal, itemCount);
    }
}
