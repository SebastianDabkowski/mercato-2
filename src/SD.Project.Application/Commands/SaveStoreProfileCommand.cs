namespace SD.Project.Application.Commands;

/// <summary>
/// Command to save store profile step data in the onboarding wizard.
/// </summary>
public sealed record SaveStoreProfileCommand(
    Guid UserId,
    string StoreName,
    string StoreDescription,
    string StoreAddress,
    string StoreCity,
    string StorePostalCode,
    string StoreCountry,
    bool CompleteStep);
