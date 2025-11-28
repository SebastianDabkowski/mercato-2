using SD.Project.Domain.Entities;

namespace SD.Project.ViewModels;

/// <summary>
/// View model used to display product data on Razor Pages.
/// </summary>
public sealed record ProductViewModel(
    Guid Id,
    string Name,
    decimal Amount,
    string Currency,
    int Stock,
    string Category,
    ProductStatus Status,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    /// <summary>
    /// Gets a display-friendly status string.
    /// </summary>
    public string StatusDisplay => Status switch
    {
        ProductStatus.Draft => "Draft",
        ProductStatus.Active => "Active",
        ProductStatus.Inactive => "Inactive",
        _ => "Unknown"
    };
}
