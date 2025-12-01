namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO for a consent type.
/// </summary>
public record ConsentTypeDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    bool IsActive,
    bool AllowPreselection,
    bool IsRequired,
    int DisplayOrder,
    ConsentVersionDto? CurrentVersion);

/// <summary>
/// DTO for a consent version.
/// </summary>
public record ConsentVersionDto(
    Guid Id,
    Guid ConsentTypeId,
    string Version,
    string ConsentText,
    bool IsCurrent,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo);

/// <summary>
/// DTO for a user consent record.
/// </summary>
public record UserConsentDto(
    Guid Id,
    Guid UserId,
    Guid ConsentTypeId,
    string ConsentTypeCode,
    string ConsentTypeName,
    Guid ConsentVersionId,
    string ConsentVersion,
    bool IsGranted,
    DateTime ConsentedAt,
    DateTime? WithdrawnAt,
    string Source,
    bool IsActive);

/// <summary>
/// DTO for user consent with full consent type details.
/// </summary>
public record UserConsentDetailDto(
    Guid Id,
    Guid UserId,
    ConsentTypeDto ConsentType,
    ConsentVersionDto ConsentVersion,
    bool IsGranted,
    DateTime ConsentedAt,
    DateTime? WithdrawnAt,
    string Source,
    bool IsActive);

/// <summary>
/// DTO for user consent audit log entry.
/// </summary>
public record UserConsentAuditLogDto(
    Guid Id,
    Guid UserConsentId,
    Guid UserId,
    string Action,
    string ConsentVersion,
    string Source,
    DateTime CreatedAt);

/// <summary>
/// Result DTO for user consent operations.
/// </summary>
public record UserConsentResultDto(
    bool Success,
    string? Message,
    IReadOnlyList<string> Errors,
    UserConsentDto? Consent)
{
    public static UserConsentResultDto Succeeded(UserConsentDto consent, string? message = null)
        => new(true, message, Array.Empty<string>(), consent);

    public static UserConsentResultDto Failed(string error)
        => new(false, null, new[] { error }, null);

    public static UserConsentResultDto Failed(IReadOnlyList<string> errors)
        => new(false, null, errors, null);
}

/// <summary>
/// Result DTO for bulk consent operations.
/// </summary>
public record BulkConsentResultDto(
    bool Success,
    string? Message,
    IReadOnlyList<string> Errors,
    IReadOnlyList<UserConsentDto> Consents)
{
    public static BulkConsentResultDto Succeeded(IReadOnlyList<UserConsentDto> consents, string? message = null)
        => new(true, message, Array.Empty<string>(), consents);

    public static BulkConsentResultDto Failed(string error)
        => new(false, null, new[] { error }, Array.Empty<UserConsentDto>());

    public static BulkConsentResultDto Failed(IReadOnlyList<string> errors)
        => new(false, null, errors, Array.Empty<UserConsentDto>());
}

/// <summary>
/// Result DTO for consent eligibility check.
/// </summary>
public record ConsentEligibilityDto(
    bool HasConsent,
    string ConsentTypeCode,
    DateTime? ConsentedAt,
    string? ConsentVersion);
