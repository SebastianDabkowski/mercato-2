using SD.Project.Domain.Entities;

namespace SD.Project.ViewModels;

/// <summary>
/// View model used to display product data on Razor Pages.
/// </summary>
public sealed record ProductViewModel(
    Guid Id,
    string Name,
    string Description,
    decimal Amount,
    string Currency,
    int Stock,
    string Category,
    ProductStatus Status,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    decimal? WeightKg,
    decimal? LengthCm,
    decimal? WidthCm,
    decimal? HeightCm)
{
    /// <summary>
    /// Gets a display-friendly status string.
    /// </summary>
    public string StatusDisplay => Status switch
    {
        ProductStatus.Draft => "Draft",
        ProductStatus.Active => "Active",
        ProductStatus.Inactive => "Inactive",
        ProductStatus.Archived => "Archived",
        _ => "Unknown"
    };
}
