namespace SD.Project.Application.Commands;

/// <summary>
/// Command describing a request to create a new product.
/// </summary>
public sealed record CreateProductCommand(
    Guid StoreId,
    string Name,
    string? Description,
    decimal Amount,
    string Currency,
    int Stock,
    string Category,
    decimal? WeightKg,
    decimal? LengthCm,
    decimal? WidthCm,
    decimal? HeightCm);
