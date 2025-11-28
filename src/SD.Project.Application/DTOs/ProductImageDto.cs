namespace SD.Project.Application.DTOs;

/// <summary>
/// Lightweight representation of product image data for UI or API layers.
/// </summary>
public sealed record ProductImageDto(
    Guid Id,
    Guid ProductId,
    string FileName,
    string ImageUrl,
    string ThumbnailUrl,
    bool IsMain,
    int DisplayOrder,
    DateTime CreatedAt);
