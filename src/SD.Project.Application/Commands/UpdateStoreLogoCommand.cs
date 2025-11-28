namespace SD.Project.Application.Commands;

/// <summary>
/// Command to update a store logo.
/// </summary>
public sealed record UpdateStoreLogoCommand(
    Guid SellerId,
    string? LogoUrl);
