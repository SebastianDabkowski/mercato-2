using SD.Project.Domain.Entities;

namespace SD.Project.Application.DTOs;

/// <summary>
/// Lightweight representation of a category attribute for UI or API layers.
/// </summary>
public sealed record CategoryAttributeDto(
    Guid Id,
    Guid CategoryId,
    string Name,
    AttributeType Type,
    string TypeDisplay,
    bool IsRequired,
    string? ListValues,
    int DisplayOrder,
    bool IsDeprecated,
    Guid? SharedAttributeId,
    string? SharedAttributeName,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
/// Lightweight representation of a shared attribute for UI or API layers.
/// </summary>
public sealed record SharedAttributeDto(
    Guid Id,
    string Name,
    AttributeType Type,
    string TypeDisplay,
    string? ListValues,
    string? Description,
    int LinkedCategoryCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
/// Result of creating a category attribute.
/// </summary>
public sealed record CreateCategoryAttributeResultDto(
    bool Success,
    CategoryAttributeDto? Attribute,
    IReadOnlyList<string> Errors)
{
    public static CreateCategoryAttributeResultDto Succeeded(CategoryAttributeDto attribute) =>
        new(true, attribute, Array.Empty<string>());

    public static CreateCategoryAttributeResultDto Failed(params string[] errors) =>
        new(false, null, errors);

    public static CreateCategoryAttributeResultDto Failed(IReadOnlyList<string> errors) =>
        new(false, null, errors);
}

/// <summary>
/// Result of updating a category attribute.
/// </summary>
public sealed record UpdateCategoryAttributeResultDto(
    bool Success,
    CategoryAttributeDto? Attribute,
    IReadOnlyList<string> Errors,
    string? Message = null)
{
    public static UpdateCategoryAttributeResultDto Succeeded(CategoryAttributeDto attribute, string? message = null) =>
        new(true, attribute, Array.Empty<string>(), message);

    public static UpdateCategoryAttributeResultDto Failed(params string[] errors) =>
        new(false, null, errors);

    public static UpdateCategoryAttributeResultDto Failed(IReadOnlyList<string> errors) =>
        new(false, null, errors);
}

/// <summary>
/// Result of deleting a category attribute.
/// </summary>
public sealed record DeleteCategoryAttributeResultDto(
    bool Success,
    IReadOnlyList<string> Errors)
{
    public static DeleteCategoryAttributeResultDto Succeeded() =>
        new(true, Array.Empty<string>());

    public static DeleteCategoryAttributeResultDto Failed(params string[] errors) =>
        new(false, errors);
}

/// <summary>
/// Result of creating a shared attribute.
/// </summary>
public sealed record CreateSharedAttributeResultDto(
    bool Success,
    SharedAttributeDto? Attribute,
    IReadOnlyList<string> Errors)
{
    public static CreateSharedAttributeResultDto Succeeded(SharedAttributeDto attribute) =>
        new(true, attribute, Array.Empty<string>());

    public static CreateSharedAttributeResultDto Failed(params string[] errors) =>
        new(false, null, errors);

    public static CreateSharedAttributeResultDto Failed(IReadOnlyList<string> errors) =>
        new(false, null, errors);
}

/// <summary>
/// Result of updating a shared attribute.
/// </summary>
public sealed record UpdateSharedAttributeResultDto(
    bool Success,
    SharedAttributeDto? Attribute,
    IReadOnlyList<string> Errors,
    string? Message = null)
{
    public static UpdateSharedAttributeResultDto Succeeded(SharedAttributeDto attribute, string? message = null) =>
        new(true, attribute, Array.Empty<string>(), message);

    public static UpdateSharedAttributeResultDto Failed(params string[] errors) =>
        new(false, null, errors);

    public static UpdateSharedAttributeResultDto Failed(IReadOnlyList<string> errors) =>
        new(false, null, errors);
}

/// <summary>
/// Result of deleting a shared attribute.
/// </summary>
public sealed record DeleteSharedAttributeResultDto(
    bool Success,
    IReadOnlyList<string> Errors)
{
    public static DeleteSharedAttributeResultDto Succeeded() =>
        new(true, Array.Empty<string>());

    public static DeleteSharedAttributeResultDto Failed(params string[] errors) =>
        new(false, errors);
}
