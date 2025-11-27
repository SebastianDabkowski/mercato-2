using SD.Project.Domain.Entities;
using SD.Project.Domain.ValueObjects;

namespace SD.Project.Domain.Services;

/// <summary>
/// Contains business rules for computing product prices.
/// </summary>
public sealed class ProductPricingService
{
    /// <summary>
    /// Applies domain pricing adjustments for the given product.
    /// </summary>
    public Money CalculateSalesPrice(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);
        // TODO: Replace placeholder logic with real pricing rules.
        return product.Price;
    }
}
