namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a shipping rule for a seller's store.
/// Defines how shipping costs are calculated for orders from this store.
/// </summary>
public class ShippingRule
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The store this shipping rule belongs to.
    /// </summary>
    public Guid StoreId { get; private set; }

    /// <summary>
    /// Name of the shipping rule (e.g., "Standard Shipping", "Express").
    /// </summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// Base shipping cost applied to any order.
    /// </summary>
    public decimal BaseCost { get; private set; }

    /// <summary>
    /// Additional cost per item in the order.
    /// </summary>
    public decimal CostPerItem { get; private set; }

    /// <summary>
    /// Order subtotal threshold for free shipping. Null means no free shipping threshold.
    /// </summary>
    public decimal? FreeShippingThreshold { get; private set; }

    /// <summary>
    /// Currency code for the shipping costs (e.g., "USD", "EUR").
    /// </summary>
    public string Currency { get; private set; } = default!;

    /// <summary>
    /// Whether this is the default shipping rule for the store.
    /// </summary>
    public bool IsDefault { get; private set; }

    /// <summary>
    /// Whether this shipping rule is currently active.
    /// </summary>
    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private ShippingRule()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new shipping rule for a store.
    /// </summary>
    public ShippingRule(
        Guid storeId,
        string name,
        decimal baseCost,
        decimal costPerItem,
        string currency,
        decimal? freeShippingThreshold = null,
        bool isDefault = false)
    {
        if (storeId == Guid.Empty)
        {
            throw new ArgumentException("Store ID is required.", nameof(storeId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Shipping rule name is required.", nameof(name));
        }

        if (baseCost < 0)
        {
            throw new ArgumentException("Base cost cannot be negative.", nameof(baseCost));
        }

        if (costPerItem < 0)
        {
            throw new ArgumentException("Cost per item cannot be negative.", nameof(costPerItem));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        if (freeShippingThreshold.HasValue && freeShippingThreshold.Value < 0)
        {
            throw new ArgumentException("Free shipping threshold cannot be negative.", nameof(freeShippingThreshold));
        }

        Id = Guid.NewGuid();
        StoreId = storeId;
        Name = name.Trim();
        BaseCost = baseCost;
        CostPerItem = costPerItem;
        Currency = currency.ToUpperInvariant();
        FreeShippingThreshold = freeShippingThreshold;
        IsDefault = isDefault;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Calculates the shipping cost for a given order subtotal and item count.
    /// </summary>
    /// <param name="subtotal">The order subtotal.</param>
    /// <param name="itemCount">The total number of items.</param>
    /// <returns>The calculated shipping cost.</returns>
    public decimal CalculateShippingCost(decimal subtotal, int itemCount)
    {
        if (itemCount <= 0)
        {
            return 0m;
        }

        // Check if free shipping applies
        if (FreeShippingThreshold.HasValue && subtotal >= FreeShippingThreshold.Value)
        {
            return 0m;
        }

        return BaseCost + (CostPerItem * itemCount);
    }

    /// <summary>
    /// Updates the shipping rule costs.
    /// </summary>
    public void UpdateCosts(decimal baseCost, decimal costPerItem, decimal? freeShippingThreshold)
    {
        if (baseCost < 0)
        {
            throw new ArgumentException("Base cost cannot be negative.", nameof(baseCost));
        }

        if (costPerItem < 0)
        {
            throw new ArgumentException("Cost per item cannot be negative.", nameof(costPerItem));
        }

        if (freeShippingThreshold.HasValue && freeShippingThreshold.Value < 0)
        {
            throw new ArgumentException("Free shipping threshold cannot be negative.", nameof(freeShippingThreshold));
        }

        BaseCost = baseCost;
        CostPerItem = costPerItem;
        FreeShippingThreshold = freeShippingThreshold;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the shipping rule name.
    /// </summary>
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Shipping rule name is required.", nameof(name));
        }

        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets this rule as the default for the store.
    /// </summary>
    public void SetAsDefault()
    {
        IsDefault = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes the default status from this rule.
    /// </summary>
    public void RemoveDefault()
    {
        IsDefault = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the shipping rule.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the shipping rule.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
