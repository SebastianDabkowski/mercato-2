namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO representing a store profile.
/// </summary>
public sealed record StoreDto(
    Guid Id,
    Guid SellerId,
    string Name,
    string? LogoUrl,
    string? Description,
    string ContactEmail,
    string? PhoneNumber,
    string? WebsiteUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt);
