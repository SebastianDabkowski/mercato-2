namespace SD.Project.Application.Commands;

/// <summary>
/// Command describing a request to create a new product variant.
/// </summary>
public sealed record CreateProductVariantCommand(
    Guid SellerId,
    Guid ProductId,
    string? Sku,
    int Stock,
    decimal? PriceOverrideAmount,
    string? PriceOverrideCurrency,
    string AttributeValues);

/// <summary>
/// Command describing a request to update an existing product variant.
/// </summary>
public sealed record UpdateProductVariantCommand(
    Guid SellerId,
    Guid VariantId,
    string? Sku,
    int Stock,
    decimal? PriceOverrideAmount,
    string? PriceOverrideCurrency,
    bool IsAvailable,
    string AttributeValues);

/// <summary>
/// Command describing a request to delete a product variant.
/// </summary>
public sealed record DeleteProductVariantCommand(
    Guid SellerId,
    Guid VariantId);

/// <summary>
/// Command describing a request to enable variants for a product.
/// </summary>
public sealed record EnableProductVariantsCommand(
    Guid SellerId,
    Guid ProductId);

/// <summary>
/// Command describing a request to add a variant attribute definition to a product.
/// </summary>
public sealed record AddVariantAttributeDefinitionCommand(
    Guid SellerId,
    Guid ProductId,
    string Name,
    string PossibleValues,
    int DisplayOrder = 0);
