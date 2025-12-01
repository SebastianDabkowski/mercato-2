namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a shared attribute definition that can be linked to multiple categories.
/// Changes to a shared attribute propagate to all linked category attributes.
/// </summary>
public class SharedAttribute
{
    public Guid Id { get; private set; }

    /// <summary>
    /// Name of the shared attribute (e.g., "Brand", "Material", "Country of Origin").
    /// </summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// Type of the attribute determining how it should be presented and validated.
    /// </summary>
    public AttributeType Type { get; private set; }

    /// <summary>
    /// Comma-separated list of possible values for list-type attributes.
    /// Only applicable when Type is List.
    /// </summary>
    public string? ListValues { get; private set; }

    /// <summary>
    /// Description of what this shared attribute represents.
    /// </summary>
    public string? Description { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private SharedAttribute()
    {
        // EF Core constructor
    }

    public SharedAttribute(
        Guid id,
        string name,
        AttributeType type,
        string? listValues = null,
        string? description = null)
    {
        if (id == Guid.Empty)
        {
            id = Guid.NewGuid();
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Attribute name is required", nameof(name));
        }

        if (type == AttributeType.List && string.IsNullOrWhiteSpace(listValues))
        {
            throw new ArgumentException("List values are required for list-type attributes", nameof(listValues));
        }

        Id = id;
        Name = name.Trim();
        Type = type;
        ListValues = type == AttributeType.List ? listValues?.Trim() : null;
        Description = description?.Trim();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the shared attribute name.
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
    /// Updates the description.
    /// </summary>
    public void UpdateDescription(string? description)
    {
        Description = description?.Trim();
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
