using SD.Project.Domain.Entities;

namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO representing the current state of seller onboarding.
/// </summary>
public sealed record SellerOnboardingDto(
    Guid Id,
    Guid UserId,
    OnboardingStep CurrentStep,
    OnboardingStatus Status,
    // Step 1: Store Profile
    string? StoreName,
    string? StoreDescription,
    string? StoreAddress,
    string? StoreCity,
    string? StorePostalCode,
    string? StoreCountry,
    bool StoreProfileCompleted,
    // Step 2: Verification
    string? BusinessName,
    string? BusinessRegistrationNumber,
    string? TaxIdentificationNumber,
    string? BusinessAddress,
    bool VerificationCompleted,
    // Step 3: Payout
    string? BankAccountHolder,
    string? BankAccountNumber,
    string? BankName,
    string? BankSwiftCode,
    bool PayoutCompleted,
    // Timestamps
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? SubmittedAt);
