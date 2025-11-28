namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get all images for a specific product.
/// </summary>
public sealed record GetProductImagesQuery(Guid ProductId);
