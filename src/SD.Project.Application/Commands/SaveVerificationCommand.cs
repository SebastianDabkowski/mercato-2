using SD.Project.Domain.Entities;

namespace SD.Project.Application.Commands;

/// <summary>
/// Command to save verification step data in the onboarding wizard.
/// </summary>
public sealed record SaveVerificationCommand(
    Guid UserId,
    SellerType SellerType,
    // Company verification fields
    string BusinessName,
    string BusinessRegistrationNumber,
    string TaxIdentificationNumber,
    string BusinessAddress,
    string ContactPersonName,
    string ContactPersonEmail,
    string ContactPersonPhone,
    // Individual verification fields
    string FullName,
    string PersonalIdNumber,
    string PersonalAddress,
    string PersonalEmail,
    string PersonalPhone,
    bool CompleteStep);
