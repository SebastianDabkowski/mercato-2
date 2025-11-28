namespace SD.Project.Application.Commands;

/// <summary>
/// Command describing a request to delete a product image.
/// </summary>
public sealed record DeleteProductImageCommand(
    Guid ImageId,
    Guid SellerId);
