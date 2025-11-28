using SD.Project.Domain.Entities;

namespace SD.Project.Application.DTOs;

/// <summary>
/// Lightweight representation of product data for UI or API layers.
/// </summary>
public sealed record ProductDto(
    Guid Id,
    string Name,
    string Description,
    decimal Amount,
    string Currency,
    int Stock,
    string Category,
    ProductStatus Status,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    decimal? WeightKg,
    decimal? LengthCm,
    decimal? WidthCm,
    decimal? HeightCm,
    string? Sku = null,
    string? MainImageUrl = null,
    string? MainImageThumbnailUrl = null,
    bool HasVariants = false);
