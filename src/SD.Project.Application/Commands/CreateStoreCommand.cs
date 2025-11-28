namespace SD.Project.Application.Commands;

/// <summary>
/// Command to create a new store for a seller.
/// </summary>
public sealed record CreateStoreCommand(
    Guid SellerId,
    string Name,
    string? Description,
    string ContactEmail,
    string? PhoneNumber,
    string? WebsiteUrl);
