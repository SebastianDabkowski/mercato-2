namespace SD.Project.Application.Commands;

/// <summary>
/// Command to update an existing store profile.
/// </summary>
public sealed record UpdateStoreCommand(
    Guid SellerId,
    string Name,
    string? Description,
    string ContactEmail,
    string? PhoneNumber,
    string? WebsiteUrl);
