namespace SD.Project.Application.DTOs;

/// <summary>
/// Lightweight representation of product variant data for UI or API layers.
/// </summary>
public sealed record ProductVariantDto(
    Guid Id,
    Guid ProductId,
    string? Sku,
    int Stock,
    decimal? PriceOverrideAmount,
    string? PriceOverrideCurrency,
    bool IsAvailable,
    string AttributeValues,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
/// Lightweight representation of product variant attribute definition for UI or API layers.
/// </summary>
public sealed record ProductVariantAttributeDefinitionDto(
    Guid Id,
    Guid ProductId,
    string Name,
    string PossibleValues,
    int DisplayOrder,
    DateTime CreatedAt,
    DateTime UpdatedAt);
