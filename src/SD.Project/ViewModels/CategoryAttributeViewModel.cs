using SD.Project.Domain.Entities;

namespace SD.Project.ViewModels;

/// <summary>
/// View model used to display category attribute data on Razor Pages.
/// </summary>
public sealed record CategoryAttributeViewModel(
    Guid Id,
    Guid CategoryId,
    string Name,
    AttributeType Type,
    string TypeDisplay,
    bool IsRequired,
    string? ListValues,
    int DisplayOrder,
    bool IsDeprecated,
    Guid? SharedAttributeId,
    string? SharedAttributeName,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    /// <summary>
    /// Gets a display-friendly status string.
    /// </summary>
    public string StatusDisplay => IsDeprecated ? "Deprecated" : "Active";

    /// <summary>
    /// Gets the status badge CSS class.
    /// </summary>
    public string StatusBadgeClass => IsDeprecated ? "bg-warning text-dark" : "bg-success";

    /// <summary>
    /// Gets the required display string.
    /// </summary>
    public string RequiredDisplay => IsRequired ? "Required" : "Optional";

    /// <summary>
    /// Gets the required badge CSS class.
    /// </summary>
    public string RequiredBadgeClass => IsRequired ? "bg-danger" : "bg-secondary";

    /// <summary>
    /// Gets the list values as a formatted display string.
    /// </summary>
    public string ListValuesDisplay => string.IsNullOrWhiteSpace(ListValues) ? "—" : ListValues;

    /// <summary>
    /// Gets the shared attribute display name or "—" if not linked.
    /// </summary>
    public string SharedAttributeDisplay => SharedAttributeName ?? "—";

    /// <summary>
    /// Gets whether this attribute is linked to a shared attribute.
    /// </summary>
    public bool IsLinkedToShared => SharedAttributeId.HasValue;
}

/// <summary>
/// View model used to display shared attribute data on Razor Pages.
/// </summary>
public sealed record SharedAttributeViewModel(
    Guid Id,
    string Name,
    AttributeType Type,
    string TypeDisplay,
    string? ListValues,
    string? Description,
    int LinkedCategoryCount,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    /// <summary>
    /// Gets the list values as a formatted display string.
    /// </summary>
    public string ListValuesDisplay => string.IsNullOrWhiteSpace(ListValues) ? "—" : ListValues;

    /// <summary>
    /// Gets the description or a placeholder if not set.
    /// </summary>
    public string DescriptionDisplay => string.IsNullOrWhiteSpace(Description) ? "—" : Description;
}
