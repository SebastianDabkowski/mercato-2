using SD.Project.Domain.Entities;

namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO for a legal document.
/// </summary>
public record LegalDocumentDto(
    Guid Id,
    LegalDocumentType DocumentType,
    string DocumentTypeName,
    string Title,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    LegalDocumentVersionDto? CurrentVersion,
    LegalDocumentVersionDto? ScheduledVersion);

/// <summary>
/// DTO for a legal document version.
/// </summary>
public record LegalDocumentVersionDto(
    Guid Id,
    Guid LegalDocumentId,
    string VersionNumber,
    string Content,
    string? ChangesSummary,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    bool IsPublished,
    bool IsCurrentlyActive,
    bool IsScheduled,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    Guid? CreatedBy);

/// <summary>
/// DTO for displaying legal document to end users with optional notice about upcoming changes.
/// </summary>
public record LegalDocumentPublicDto(
    LegalDocumentType DocumentType,
    string Title,
    string VersionNumber,
    string Content,
    DateTime EffectiveFrom,
    bool HasUpcomingVersion,
    DateTime? UpcomingVersionEffectiveDate,
    string? UpcomingVersionChangesSummary);

/// <summary>
/// Result DTO for legal document operations.
/// </summary>
public record LegalDocumentResultDto(
    bool Success,
    string? Message,
    IReadOnlyList<string> Errors,
    LegalDocumentDto? Document)
{
    public static LegalDocumentResultDto Succeeded(LegalDocumentDto document, string? message = null)
        => new(true, message, Array.Empty<string>(), document);

    public static LegalDocumentResultDto Failed(string error)
        => new(false, null, new[] { error }, null);

    public static LegalDocumentResultDto Failed(IReadOnlyList<string> errors)
        => new(false, null, errors, null);
}

/// <summary>
/// Result DTO for legal document version operations.
/// </summary>
public record LegalDocumentVersionResultDto(
    bool Success,
    string? Message,
    IReadOnlyList<string> Errors,
    LegalDocumentVersionDto? Version)
{
    public static LegalDocumentVersionResultDto Succeeded(LegalDocumentVersionDto version, string? message = null)
        => new(true, message, Array.Empty<string>(), version);

    public static LegalDocumentVersionResultDto Failed(string error)
        => new(false, null, new[] { error }, null);

    public static LegalDocumentVersionResultDto Failed(IReadOnlyList<string> errors)
        => new(false, null, errors, null);
}
