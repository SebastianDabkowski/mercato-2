namespace SD.Project.Application.Commands;

/// <summary>
/// Defines how a price change should be applied.
/// </summary>
public enum PriceChangeType
{
    /// <summary>No change to price.</summary>
    None,
    /// <summary>Set an exact fixed price value.</summary>
    FixedValue,
    /// <summary>Increase price by a percentage.</summary>
    PercentageUp,
    /// <summary>Decrease price by a percentage.</summary>
    PercentageDown
}

/// <summary>
/// Defines how a stock change should be applied.
/// </summary>
public enum StockChangeType
{
    /// <summary>No change to stock.</summary>
    None,
    /// <summary>Set an exact stock value.</summary>
    SetExact,
    /// <summary>Increase stock by a value.</summary>
    Increase,
    /// <summary>Decrease stock by a value.</summary>
    Decrease
}

/// <summary>
/// Command to bulk update price and/or stock for multiple products.
/// </summary>
/// <param name="SellerId">The ID of the seller performing the update.</param>
/// <param name="ProductIds">The IDs of products to update.</param>
/// <param name="PriceChangeType">How to apply the price change.</param>
/// <param name="PriceValue">The value for price change (amount or percentage).</param>
/// <param name="StockChangeType">How to apply the stock change.</param>
/// <param name="StockValue">The value for stock change.</param>
public sealed record BulkUpdatePriceAndStockCommand(
    Guid SellerId,
    IReadOnlyCollection<Guid> ProductIds,
    PriceChangeType PriceChangeType,
    decimal? PriceValue,
    StockChangeType StockChangeType,
    int? StockValue);
