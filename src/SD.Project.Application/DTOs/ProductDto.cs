namespace SD.Project.Application.DTOs;

/// <summary>
/// Lightweight representation of product data for UI or API layers.
/// </summary>
public sealed record ProductDto(Guid Id, string Name, decimal Amount, string Currency, bool IsActive);
