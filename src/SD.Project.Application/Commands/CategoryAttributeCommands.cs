using SD.Project.Domain.Entities;

namespace SD.Project.Application.Commands;

/// <summary>
/// Command to create a new category attribute.
/// </summary>
public sealed record CreateCategoryAttributeCommand(
    Guid CategoryId,
    string Name,
    AttributeType Type,
    bool IsRequired,
    string? ListValues = null,
    int DisplayOrder = 0,
    Guid? SharedAttributeId = null);

/// <summary>
/// Command to update an existing category attribute.
/// </summary>
public sealed record UpdateCategoryAttributeCommand(
    Guid AttributeId,
    string Name,
    AttributeType Type,
    bool IsRequired,
    string? ListValues = null,
    int DisplayOrder = 0);

/// <summary>
/// Command to delete a category attribute.
/// </summary>
public sealed record DeleteCategoryAttributeCommand(Guid AttributeId);

/// <summary>
/// Command to deprecate a category attribute.
/// The attribute will be hidden from future product creation but remains visible for existing products.
/// </summary>
public sealed record DeprecateCategoryAttributeCommand(Guid AttributeId);

/// <summary>
/// Command to reactivate a deprecated category attribute.
/// </summary>
public sealed record ReactivateCategoryAttributeCommand(Guid AttributeId);

/// <summary>
/// Command to link a category attribute to a shared attribute.
/// </summary>
public sealed record LinkToSharedAttributeCommand(Guid AttributeId, Guid SharedAttributeId);

/// <summary>
/// Command to unlink a category attribute from a shared attribute.
/// </summary>
public sealed record UnlinkFromSharedAttributeCommand(Guid AttributeId);

/// <summary>
/// Command to create a new shared attribute.
/// </summary>
public sealed record CreateSharedAttributeCommand(
    string Name,
    AttributeType Type,
    string? ListValues = null,
    string? Description = null);

/// <summary>
/// Command to update an existing shared attribute.
/// Updates will be reflected in all linked category attributes.
/// </summary>
public sealed record UpdateSharedAttributeCommand(
    Guid SharedAttributeId,
    string Name,
    AttributeType Type,
    string? ListValues = null,
    string? Description = null);

/// <summary>
/// Command to delete a shared attribute.
/// </summary>
public sealed record DeleteSharedAttributeCommand(Guid SharedAttributeId);
