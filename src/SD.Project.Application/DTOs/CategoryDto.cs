namespace SD.Project.Application.DTOs;

/// <summary>
/// Lightweight representation of category data for UI or API layers.
/// </summary>
public sealed record CategoryDto(
    Guid Id,
    string Name,
    Guid? ParentId,
    string? ParentName,
    int DisplayOrder,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int ProductCount,
    int ChildCount);
