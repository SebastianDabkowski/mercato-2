namespace SD.Project.Application.Commands;

/// <summary>
/// Command to create a new data processing activity.
/// </summary>
public record CreateDataProcessingActivityCommand(
    string Name,
    string Purpose,
    string LegalBasis,
    string DataCategories,
    string DataSubjects,
    string RetentionPeriod,
    Guid CreatedByUserId,
    string? Description = null,
    string? Processors = null,
    string? InternationalTransfers = null,
    string? SecurityMeasures = null);

/// <summary>
/// Command to update an existing data processing activity.
/// </summary>
public record UpdateDataProcessingActivityCommand(
    Guid Id,
    string Name,
    string Purpose,
    string LegalBasis,
    string DataCategories,
    string DataSubjects,
    string RetentionPeriod,
    Guid ModifiedByUserId,
    string? Description = null,
    string? Processors = null,
    string? InternationalTransfers = null,
    string? SecurityMeasures = null,
    string? ChangeReason = null);

/// <summary>
/// Command to archive a data processing activity.
/// </summary>
public record ArchiveDataProcessingActivityCommand(
    Guid Id,
    Guid ModifiedByUserId,
    string? ChangeReason = null);

/// <summary>
/// Command to reactivate an archived data processing activity.
/// </summary>
public record ReactivateDataProcessingActivityCommand(
    Guid Id,
    Guid ModifiedByUserId,
    string? ChangeReason = null);
