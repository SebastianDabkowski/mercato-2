namespace SD.Project.Application.Commands;

/// <summary>
/// Command describing a request to create a new product.
/// </summary>
public sealed record CreateProductCommand(
    Guid StoreId,
    string Name,
    decimal Amount,
    string Currency,
    int Stock,
    string Category);
