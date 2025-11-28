using SD.Project.Domain.Entities;

namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO representing a store profile.
/// </summary>
public sealed record StoreDto(
    Guid Id,
    Guid SellerId,
    string Name,
    string Slug,
    string? LogoUrl,
    string? Description,
    string ContactEmail,
    string? PhoneNumber,
    string? WebsiteUrl,
    StoreStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    /// <summary>
    /// Determines if the store is publicly visible.
    /// </summary>
    public bool IsPubliclyVisible => Status == StoreStatus.Active || Status == StoreStatus.LimitedActive;
}
