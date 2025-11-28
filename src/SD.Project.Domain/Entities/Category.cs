namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a product category in the catalog hierarchy.
/// Categories can have parent-child relationships to form a tree structure.
/// </summary>
public class Category
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public Guid? ParentId { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Category()
    {
        // EF Core constructor
    }

    public Category(Guid id, string name, Guid? parentId = null, int displayOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name is required", nameof(name));
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Name = name.Trim();
        ParentId = parentId;
        DisplayOrder = displayOrder;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the category name.
    /// </summary>
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name is required", nameof(name));
        }

        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the parent category.
    /// </summary>
    public void UpdateParent(Guid? parentId)
    {
        if (parentId == Id)
        {
            throw new ArgumentException("A category cannot be its own parent", nameof(parentId));
        }

        ParentId = parentId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the display order of the category.
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
    /// Activates the category making it available for product assignment.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the category. Deactivated categories are not available for new product assignments.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
