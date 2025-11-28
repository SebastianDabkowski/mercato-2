namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the workflow state of a product.
/// </summary>
public enum ProductStatus
{
    /// <summary>
    /// Product is not yet visible in the public catalog.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Product is visible and available for purchase.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Product is hidden from the public catalog.
    /// </summary>
    Inactive = 2
}
