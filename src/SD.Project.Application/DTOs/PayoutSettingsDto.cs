using SD.Project.Domain.Entities;

namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO representing the current state of seller payout settings.
/// </summary>
public sealed record PayoutSettingsDto(
    Guid Id,
    Guid SellerId,
    PayoutMethod DefaultPayoutMethod,
    // Bank transfer details
    string? BankAccountHolder,
    string? BankAccountNumber,
    string? BankName,
    string? BankSwiftCode,
    string? BankCountry,
    // SEPA details
    string? SepaIban,
    string? SepaBic,
    // Status
    bool IsConfigured,
    bool IsVerified,
    bool IsBankTransferAvailable,
    bool IsSepaAvailable,
    // Timestamps
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? VerifiedAt);
