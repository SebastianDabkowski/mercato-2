namespace SD.Project.ViewModels;

/// <summary>
/// View model for product filter state on search and category pages.
/// </summary>
public sealed class ProductFilterViewModel
{
    /// <summary>
    /// Filter by category name.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Minimum price filter (inclusive).
    /// </summary>
    public decimal? MinPrice { get; set; }

    /// <summary>
    /// Maximum price filter (inclusive).
    /// </summary>
    public decimal? MaxPrice { get; set; }

    /// <summary>
    /// Filter by store/seller ID.
    /// </summary>
    public Guid? StoreId { get; set; }

    /// <summary>
    /// Determines if any filters are currently applied.
    /// </summary>
    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(Category) ||
        MinPrice.HasValue ||
        MaxPrice.HasValue ||
        StoreId.HasValue;

    /// <summary>
    /// Creates a copy of the filter with all values cleared.
    /// </summary>
    public ProductFilterViewModel Clear() => new();
}
