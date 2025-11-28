namespace SD.Project.Application.Commands;

/// <summary>
/// Command describing a request to set an image as the main product image.
/// </summary>
public sealed record SetMainProductImageCommand(
    Guid ImageId,
    Guid SellerId);
