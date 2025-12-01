namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get all attributes for a specific category.
/// </summary>
public sealed record GetCategoryAttributesQuery(Guid CategoryId, bool IncludeDeprecated = false);

/// <summary>
/// Query to get a specific category attribute by ID.
/// </summary>
public sealed record GetCategoryAttributeByIdQuery(Guid AttributeId);

/// <summary>
/// Query to get all shared attributes.
/// </summary>
public sealed record GetAllSharedAttributesQuery;

/// <summary>
/// Query to get a specific shared attribute by ID.
/// </summary>
public sealed record GetSharedAttributeByIdQuery(Guid SharedAttributeId);
