namespace SD.Project.Application.Commands;

/// <summary>
/// Command describing a request to update an existing product.
/// </summary>
public sealed record UpdateProductCommand(
    Guid ProductId,
    Guid SellerId,
    string Name,
    decimal Amount,
    string Currency,
    int Stock,
    string Category);
