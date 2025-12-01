namespace SD.Project.Application.Commands;

/// <summary>
/// Command to record a user's consent decision.
/// </summary>
public record RecordUserConsentCommand(
    Guid UserId,
    Guid ConsentTypeId,
    bool IsGranted,
    string Source,
    string? IpAddress = null,
    string? UserAgent = null);

/// <summary>
/// Command to record multiple consent decisions at once (e.g., during registration).
/// </summary>
public record RecordBulkUserConsentsCommand(
    Guid UserId,
    IReadOnlyList<ConsentDecision> Decisions,
    string Source,
    string? IpAddress = null,
    string? UserAgent = null);

/// <summary>
/// Represents a single consent decision in a bulk operation.
/// </summary>
public record ConsentDecision(
    Guid ConsentTypeId,
    bool IsGranted);

/// <summary>
/// Command to withdraw a user's consent.
/// </summary>
public record WithdrawUserConsentCommand(
    Guid UserId,
    Guid ConsentTypeId,
    string Source,
    string? IpAddress = null,
    string? UserAgent = null);

/// <summary>
/// Command to create a new consent type (admin operation).
/// </summary>
public record CreateConsentTypeCommand(
    string Code,
    string Name,
    string Description,
    bool AllowPreselection = false,
    bool IsRequired = false,
    int DisplayOrder = 0,
    string? InitialConsentText = null);

/// <summary>
/// Command to update a consent type (admin operation).
/// </summary>
public record UpdateConsentTypeCommand(
    Guid Id,
    string Name,
    string Description,
    bool AllowPreselection,
    bool IsRequired,
    int DisplayOrder);

/// <summary>
/// Command to create a new consent version (when consent text changes).
/// </summary>
public record CreateConsentVersionCommand(
    Guid ConsentTypeId,
    string Version,
    string ConsentText,
    DateTime EffectiveFrom);
