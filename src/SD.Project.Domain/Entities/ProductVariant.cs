namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a specific variant of a product (e.g., a specific size/color combination).
/// Each variant can have its own SKU, stock, price override, and images.
/// </summary>
public class ProductVariant
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }

    /// <summary>
    /// Stock Keeping Unit - unique identifier for this specific variant.
    /// </summary>
    public string? Sku { get; private set; }

    /// <summary>
    /// Stock quantity available for this variant.
    /// </summary>
    public int Stock { get; private set; }

    /// <summary>
    /// Optional price override for this variant. If null, uses the product's base price.
    /// </summary>
    public ValueObjects.Money? PriceOverride { get; private set; }

    /// <summary>
    /// Indicates if this variant is available for purchase.
    /// </summary>
    public bool IsAvailable { get; private set; }

    /// <summary>
    /// JSON-serialized dictionary of attribute name-value pairs (e.g., {"Size": "XL", "Color": "Blue"}).
    /// </summary>
    public string AttributeValues { get; private set; } = "{}";

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private ProductVariant()
    {
        // EF Core constructor
    }

    public ProductVariant(Guid id, Guid productId, string attributeValues)
    {
        if (id == Guid.Empty)
        {
            id = Guid.NewGuid();
        }

        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));
        }

        if (string.IsNullOrWhiteSpace(attributeValues))
        {
            throw new ArgumentException("Attribute values are required", nameof(attributeValues));
        }

        Id = id;
        ProductId = productId;
        AttributeValues = attributeValues;
        Stock = 0;
        IsAvailable = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the SKU for this variant.
    /// </summary>
    public void UpdateSku(string? sku)
    {
        Sku = sku?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the stock quantity for this variant.
    /// </summary>
    public void UpdateStock(int stock)
    {
        if (stock < 0)
        {
            throw new ArgumentException("Stock cannot be negative", nameof(stock));
        }

        Stock = stock;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets a price override for this variant.
    /// </summary>
    public void SetPriceOverride(ValueObjects.Money? priceOverride)
    {
        PriceOverride = priceOverride;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the attribute values for this variant.
    /// </summary>
    public void UpdateAttributeValues(string attributeValues)
    {
        if (string.IsNullOrWhiteSpace(attributeValues))
        {
            throw new ArgumentException("Attribute values are required", nameof(attributeValues));
        }

        AttributeValues = attributeValues;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks this variant as available for purchase.
    /// </summary>
    public void MarkAsAvailable()
    {
        IsAvailable = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks this variant as unavailable for purchase.
    /// </summary>
    public void MarkAsUnavailable()
    {
        IsAvailable = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if this variant can be added to cart (available and in stock).
    /// </summary>
    public bool CanAddToCart()
    {
        return IsAvailable && Stock > 0;
    }
}
