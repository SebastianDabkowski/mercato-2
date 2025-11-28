namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get all variants for a product.
/// </summary>
public sealed record GetProductVariantsQuery(Guid ProductId);

/// <summary>
/// Query to get available variants for a product (for buyer view).
/// </summary>
public sealed record GetAvailableProductVariantsQuery(Guid ProductId);

/// <summary>
/// Query to get a specific variant by ID.
/// </summary>
public sealed record GetProductVariantByIdQuery(Guid VariantId);

/// <summary>
/// Query to get variant attribute definitions for a product.
/// </summary>
public sealed record GetVariantAttributeDefinitionsQuery(Guid ProductId);
