namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a shipping method/option available for a seller's store.
/// This is separate from ShippingRule which defines cost calculation.
/// ShippingMethod represents the actual shipping option presented to buyers.
/// </summary>
public class ShippingMethod
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The store this shipping method belongs to.
    /// Null for platform-wide shipping methods.
    /// </summary>
    public Guid? StoreId { get; private set; }

    /// <summary>
    /// Associated shipping rule for cost calculation.
    /// </summary>
    public Guid? ShippingRuleId { get; private set; }

    /// <summary>
    /// Name of the shipping method (e.g., "Standard Shipping", "Express Delivery").
    /// </summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// Description of the shipping method.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Carrier/courier name (e.g., "UPS", "FedEx", "USPS").
    /// </summary>
    public string? CarrierName { get; private set; }

    /// <summary>
    /// Estimated delivery time in business days (minimum).
    /// </summary>
    public int EstimatedDeliveryDaysMin { get; private set; }

    /// <summary>
    /// Estimated delivery time in business days (maximum).
    /// </summary>
    public int EstimatedDeliveryDaysMax { get; private set; }

    /// <summary>
    /// Base shipping cost.
    /// </summary>
    public decimal BaseCost { get; private set; }

    /// <summary>
    /// Additional cost per item.
    /// </summary>
    public decimal CostPerItem { get; private set; }

    /// <summary>
    /// Order subtotal threshold for free shipping. Null means no free shipping.
    /// </summary>
    public decimal? FreeShippingThreshold { get; private set; }

    /// <summary>
    /// Currency code for costs.
    /// </summary>
    public string Currency { get; private set; } = default!;

    /// <summary>
    /// Display order for sorting.
    /// </summary>
    public int DisplayOrder { get; private set; }

    /// <summary>
    /// Whether this is the default shipping method for the store.
    /// </summary>
    public bool IsDefault { get; private set; }

    /// <summary>
    /// Whether this shipping method is currently active.
    /// </summary>
    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private ShippingMethod()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new shipping method for a store.
    /// </summary>
    public ShippingMethod(
        Guid? storeId,
        string name,
        string? description,
        string? carrierName,
        int estimatedDeliveryDaysMin,
        int estimatedDeliveryDaysMax,
        decimal baseCost,
        decimal costPerItem,
        string currency,
        decimal? freeShippingThreshold = null,
        int displayOrder = 0,
        bool isDefault = false,
        Guid? shippingRuleId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Shipping method name is required.", nameof(name));
        }

        if (estimatedDeliveryDaysMin < 0)
        {
            throw new ArgumentException("Minimum delivery days cannot be negative.", nameof(estimatedDeliveryDaysMin));
        }

        if (estimatedDeliveryDaysMax < estimatedDeliveryDaysMin)
        {
            throw new ArgumentException("Maximum delivery days cannot be less than minimum.", nameof(estimatedDeliveryDaysMax));
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

        Id = Guid.NewGuid();
        StoreId = storeId;
        ShippingRuleId = shippingRuleId;
        Name = name.Trim();
        Description = description?.Trim();
        CarrierName = carrierName?.Trim();
        EstimatedDeliveryDaysMin = estimatedDeliveryDaysMin;
        EstimatedDeliveryDaysMax = estimatedDeliveryDaysMax;
        BaseCost = baseCost;
        CostPerItem = costPerItem;
        Currency = currency.ToUpperInvariant();
        FreeShippingThreshold = freeShippingThreshold;
        DisplayOrder = displayOrder;
        IsDefault = isDefault;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Calculates the shipping cost for a given order subtotal and item count.
    /// </summary>
    public decimal CalculateCost(decimal subtotal, int itemCount)
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
    /// Gets the estimated delivery date range.
    /// </summary>
    public (DateTime MinDate, DateTime MaxDate) GetEstimatedDeliveryRange()
    {
        var today = DateTime.UtcNow.Date;
        return (today.AddDays(EstimatedDeliveryDaysMin), today.AddDays(EstimatedDeliveryDaysMax));
    }

    /// <summary>
    /// Gets a formatted delivery time string.
    /// </summary>
    public string GetDeliveryTimeDisplay()
    {
        if (EstimatedDeliveryDaysMin == EstimatedDeliveryDaysMax)
        {
            return EstimatedDeliveryDaysMin == 1 
                ? "1 business day" 
                : $"{EstimatedDeliveryDaysMin} business days";
        }

        return $"{EstimatedDeliveryDaysMin}-{EstimatedDeliveryDaysMax} business days";
    }

    /// <summary>
    /// Updates the shipping method details.
    /// </summary>
    public void Update(
        string name,
        string? description,
        string? carrierName,
        int estimatedDeliveryDaysMin,
        int estimatedDeliveryDaysMax,
        int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Shipping method name is required.", nameof(name));
        }

        Name = name.Trim();
        Description = description?.Trim();
        CarrierName = carrierName?.Trim();
        EstimatedDeliveryDaysMin = estimatedDeliveryDaysMin;
        EstimatedDeliveryDaysMax = estimatedDeliveryDaysMax;
        DisplayOrder = displayOrder;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the cost structure.
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

        BaseCost = baseCost;
        CostPerItem = costPerItem;
        FreeShippingThreshold = freeShippingThreshold;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the shipping method.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the shipping method.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        IsDefault = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets this as the default shipping method.
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
