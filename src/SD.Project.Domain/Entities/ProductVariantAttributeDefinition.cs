namespace SD.Project.Domain.Entities;

/// <summary>
/// Defines a variant attribute type for a product (e.g., Size, Color).
/// This defines what attributes are available for variants, not the actual variant values.
/// </summary>
public class ProductVariantAttributeDefinition
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }

    /// <summary>
    /// Name of the attribute (e.g., "Size", "Color").
    /// </summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// Comma-separated list of possible values for this attribute (e.g., "S,M,L,XL" or "Red,Blue,Green").
    /// </summary>
    public string PossibleValues { get; private set; } = default!;

    /// <summary>
    /// Display order for this attribute when showing variant options.
    /// </summary>
    public int DisplayOrder { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private ProductVariantAttributeDefinition()
    {
        // EF Core constructor
    }

    public ProductVariantAttributeDefinition(Guid id, Guid productId, string name, string possibleValues, int displayOrder = 0)
    {
        if (id == Guid.Empty)
        {
            id = Guid.NewGuid();
        }

        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Attribute name is required", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(possibleValues))
        {
            throw new ArgumentException("Possible values are required", nameof(possibleValues));
        }

        if (displayOrder < 0)
        {
            throw new ArgumentException("Display order cannot be negative", nameof(displayOrder));
        }

        Id = id;
        ProductId = productId;
        Name = name.Trim();
        PossibleValues = possibleValues.Trim();
        DisplayOrder = displayOrder;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the attribute name.
    /// </summary>
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Attribute name is required", nameof(name));
        }

        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the possible values for this attribute.
    /// </summary>
    public void UpdatePossibleValues(string possibleValues)
    {
        if (string.IsNullOrWhiteSpace(possibleValues))
        {
            throw new ArgumentException("Possible values are required", nameof(possibleValues));
        }

        PossibleValues = possibleValues.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the display order.
    /// </summary>
    public void UpdateDisplayOrder(int displayOrder)
    {
        if (displayOrder < 0)
        {
            throw new ArgumentException("Display order cannot be negative", nameof(displayOrder));
        }

        DisplayOrder = displayOrder;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the possible values as an array.
    /// </summary>
    public string[] GetPossibleValuesArray()
    {
        return PossibleValues.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }
}
