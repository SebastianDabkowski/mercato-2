namespace SD.Project.Application.Commands;

/// <summary>
/// Command describing a request to delete (archive) a product.
/// </summary>
public sealed record DeleteProductCommand(
    Guid ProductId,
    Guid SellerId);
