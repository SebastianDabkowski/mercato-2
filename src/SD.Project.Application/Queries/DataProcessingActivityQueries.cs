namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get all data processing activities.
/// </summary>
public record GetAllDataProcessingActivitiesQuery(bool IncludeArchived = false);

/// <summary>
/// Query to get a data processing activity by ID.
/// </summary>
public record GetDataProcessingActivityByIdQuery(Guid Id);

/// <summary>
/// Query to get audit logs for a data processing activity.
/// </summary>
public record GetDataProcessingActivityAuditLogsQuery(Guid DataProcessingActivityId);

/// <summary>
/// Query to export data processing activities to CSV.
/// </summary>
public record ExportDataProcessingActivitiesQuery(bool IncludeArchived = false);
