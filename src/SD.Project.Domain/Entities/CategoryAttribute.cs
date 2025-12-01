namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents an attribute template defined for a category.
/// Attributes define the structured fields that products in this category should have.
/// </summary>
public class CategoryAttribute
{
    public Guid Id { get; private set; }
    public Guid CategoryId { get; private set; }

    /// <summary>
    /// Name of the attribute (e.g., "Size", "Color", "Material").
    /// </summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// Type of the attribute determining how it should be presented and validated.
    /// </summary>
    public AttributeType Type { get; private set; }

    /// <summary>
    /// Whether this attribute is required when creating products in this category.
    /// </summary>
    public bool IsRequired { get; private set; }

    /// <summary>
    /// Comma-separated list of possible values for list-type attributes.
    /// Only applicable when Type is List.
    /// </summary>
    public string? ListValues { get; private set; }

    /// <summary>
    /// Display order for this attribute in forms and displays.
    /// </summary>
    public int DisplayOrder { get; private set; }

    /// <summary>
    /// Whether this attribute is deprecated and should be hidden from new product creation.
    /// Deprecated attributes remain visible for existing products and reports.
    /// </summary>
    public bool IsDeprecated { get; private set; }

    /// <summary>
    /// Optional ID of a shared attribute definition that this attribute links to.
    /// When set, changes to the shared definition propagate to all linked attributes.
    /// </summary>
    public Guid? SharedAttributeId { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private CategoryAttribute()
    {
        // EF Core constructor
    }

    public CategoryAttribute(
        Guid id,
        Guid categoryId,
        string name,
        AttributeType type,
        bool isRequired = false,
        string? listValues = null,
        int displayOrder = 0,
        Guid? sharedAttributeId = null)
    {
        if (id == Guid.Empty)
        {
            id = Guid.NewGuid();
        }

        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("Category ID cannot be empty", nameof(categoryId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Attribute name is required", nameof(name));
        }

        if (displayOrder < 0)
        {
            throw new ArgumentException("Display order cannot be negative", nameof(displayOrder));
        }

        if (type == AttributeType.List && string.IsNullOrWhiteSpace(listValues))
        {
            throw new ArgumentException("List values are required for list-type attributes", nameof(listValues));
        }

        Id = id;
        CategoryId = categoryId;
        Name = name.Trim();
        Type = type;
        IsRequired = isRequired;
        ListValues = type == AttributeType.List ? listValues?.Trim() : null;
        DisplayOrder = displayOrder;
        IsDeprecated = false;
        SharedAttributeId = sharedAttributeId;
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
    /// Updates the attribute type.
    /// </summary>
    public void UpdateType(AttributeType type, string? listValues = null)
    {
        if (type == AttributeType.List && string.IsNullOrWhiteSpace(listValues))
        {
            throw new ArgumentException("List values are required for list-type attributes", nameof(listValues));
        }

        Type = type;
        ListValues = type == AttributeType.List ? listValues?.Trim() : null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the required flag.
    /// </summary>
    public void UpdateRequired(bool isRequired)
    {
        IsRequired = isRequired;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the list values for list-type attributes.
    /// </summary>
    public void UpdateListValues(string? listValues)
    {
        if (Type == AttributeType.List && string.IsNullOrWhiteSpace(listValues))
        {
            throw new ArgumentException("List values are required for list-type attributes", nameof(listValues));
        }

        ListValues = Type == AttributeType.List ? listValues?.Trim() : null;
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
    /// Marks the attribute as deprecated. Deprecated attributes are hidden from
    /// future product creation but remain visible for existing products and reports.
    /// </summary>
    public void Deprecate()
    {
        IsDeprecated = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reactivates a deprecated attribute, making it available for new product creation again.
    /// </summary>
    public void Reactivate()
    {
        IsDeprecated = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Links this attribute to a shared attribute definition.
    /// </summary>
    public void LinkToSharedAttribute(Guid sharedAttributeId)
    {
        if (sharedAttributeId == Guid.Empty)
        {
            throw new ArgumentException("Shared attribute ID cannot be empty", nameof(sharedAttributeId));
        }

        SharedAttributeId = sharedAttributeId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes the link to a shared attribute definition.
    /// </summary>
    public void UnlinkFromSharedAttribute()
    {
        SharedAttributeId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the list values as an array.
    /// </summary>
    public string[] GetListValuesArray()
    {
        if (Type != AttributeType.List || string.IsNullOrWhiteSpace(ListValues))
        {
            return Array.Empty<string>();
        }

        return ListValues.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }
}

/// <summary>
/// Defines the type of a category attribute.
/// </summary>
public enum AttributeType
{
    /// <summary>
    /// Free-form text input.
    /// </summary>
    Text = 0,

    /// <summary>
    /// Numeric value input.
    /// </summary>
    Number = 1,

    /// <summary>
    /// Selection from a predefined list of values.
    /// </summary>
    List = 2,

    /// <summary>
    /// Boolean yes/no value.
    /// </summary>
    Boolean = 3,

    /// <summary>
    /// Date value.
    /// </summary>
    Date = 4
}
