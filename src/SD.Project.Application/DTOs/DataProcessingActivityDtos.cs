namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO for a data processing activity record.
/// </summary>
public record DataProcessingActivityDto(
    Guid Id,
    string Name,
    string Description,
    string Purpose,
    string LegalBasis,
    string DataCategories,
    string DataSubjects,
    string Processors,
    string RetentionPeriod,
    string? InternationalTransfers,
    string? SecurityMeasures,
    bool IsActive,
    Guid CreatedByUserId,
    string? CreatedByUserName,
    Guid? LastModifiedByUserId,
    string? LastModifiedByUserName,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
/// DTO for a data processing activity audit log entry.
/// </summary>
public record DataProcessingActivityAuditLogDto(
    Guid Id,
    Guid DataProcessingActivityId,
    Guid UserId,
    string UserName,
    string Action,
    string? ChangeReason,
    DateTime CreatedAt);

/// <summary>
/// Result DTO for data processing activity operations.
/// </summary>
public record DataProcessingActivityResultDto(
    bool Success,
    string? Message,
    IReadOnlyList<string> Errors,
    DataProcessingActivityDto? Activity)
{
    public static DataProcessingActivityResultDto Succeeded(DataProcessingActivityDto activity, string? message = null)
        => new(true, message, Array.Empty<string>(), activity);

    public static DataProcessingActivityResultDto Failed(string error)
        => new(false, null, new[] { error }, null);

    public static DataProcessingActivityResultDto Failed(IReadOnlyList<string> errors)
        => new(false, null, errors, null);
}

/// <summary>
/// Result DTO for export operations.
/// </summary>
public record DataProcessingActivityExportResultDto(
    bool Success,
    string? Message,
    IReadOnlyList<string> Errors,
    byte[]? FileContent,
    string? FileName,
    string? ContentType)
{
    public static DataProcessingActivityExportResultDto Succeeded(byte[] content, string fileName, string contentType)
        => new(true, "Export completed successfully.", Array.Empty<string>(), content, fileName, contentType);

    public static DataProcessingActivityExportResultDto Failed(string error)
        => new(false, null, new[] { error }, null, null, null);
}
