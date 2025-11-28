namespace SD.Project.Application.Commands;

/// <summary>
/// Command to save verification step data in the onboarding wizard.
/// </summary>
public sealed record SaveVerificationCommand(
    Guid UserId,
    string BusinessName,
    string BusinessRegistrationNumber,
    string TaxIdentificationNumber,
    string BusinessAddress,
    bool CompleteStep);
