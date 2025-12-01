namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO for SLA configuration.
/// </summary>
public sealed record SlaConfigurationDto(
    Guid Id,
    string Category,
    int FirstResponseHours,
    int ResolutionHours,
    bool IsEnabled,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
/// DTO for SLA status of a case.
/// </summary>
public sealed record CaseSlaStatusDto(
    Guid ReturnRequestId,
    string CaseNumber,
    DateTime? FirstResponseDeadline,
    DateTime? ResolutionDeadline,
    DateTime? FirstRespondedAt,
    bool SlaBreached,
    DateTime? SlaBreachedAt,
    string? SlaBreachType,
    bool IsFirstResponseOverdue,
    bool IsResolutionOverdue,
    TimeSpan? TimeToFirstResponseDeadline,
    TimeSpan? TimeToResolutionDeadline);

/// <summary>
/// DTO for seller SLA statistics.
/// </summary>
public sealed record SellerSlaStatisticsDto(
    Guid StoreId,
    string StoreName,
    int TotalCases,
    int CasesWithSla,
    int CasesResolvedWithinSla,
    int CasesWithSlaBreached,
    int FirstResponseBreaches,
    int ResolutionBreaches,
    decimal PercentageResolvedWithinSla,
    TimeSpan AverageFirstResponseTime,
    TimeSpan AverageResolutionTime);

/// <summary>
/// DTO for aggregate SLA statistics on the admin dashboard.
/// </summary>
public sealed record SlaDashboardDto(
    DateTime PeriodStart,
    DateTime PeriodEnd,
    int TotalCases,
    int CasesWithSla,
    int CasesResolvedWithinSla,
    int CasesWithSlaBreached,
    decimal OverallSlaComplianceRate,
    TimeSpan AverageFirstResponseTime,
    TimeSpan AverageResolutionTime,
    IReadOnlyList<SellerSlaStatisticsDto> SellerStatistics,
    IReadOnlyList<SlaBreachedCaseSummaryDto> RecentBreaches);

/// <summary>
/// DTO for summary of a breached case.
/// </summary>
public sealed record SlaBreachedCaseSummaryDto(
    Guid ReturnRequestId,
    string CaseNumber,
    string StoreName,
    string BreachType,
    DateTime BreachedAt,
    string Status,
    int DaysOverdue);

/// <summary>
/// Result DTO for creating or updating SLA configuration.
/// </summary>
public sealed record SlaConfigurationResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    Guid? ConfigurationId = null);

/// <summary>
/// Result DTO for SLA breach check.
/// </summary>
public sealed record SlaBreachCheckResultDto(
    int CasesChecked,
    int NewBreachesDetected,
    IReadOnlyList<Guid> BreachedCaseIds);
