namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get all SLA configurations.
/// </summary>
public sealed record GetSlaConfigurationsQuery();

/// <summary>
/// Query to get a specific SLA configuration by ID.
/// </summary>
public sealed record GetSlaConfigurationByIdQuery(Guid ConfigurationId);

/// <summary>
/// Query to get SLA status for a specific case.
/// </summary>
public sealed record GetCaseSlaStatusQuery(Guid ReturnRequestId);

/// <summary>
/// Query to get SLA statistics for a specific seller/store.
/// </summary>
public sealed record GetSellerSlaStatisticsQuery(
    Guid StoreId,
    DateTime? FromDate = null,
    DateTime? ToDate = null);

/// <summary>
/// Query to get the SLA dashboard with aggregate statistics.
/// Used by admins to monitor platform-wide SLA compliance.
/// </summary>
public sealed record GetSlaDashboardQuery(
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int TopSellersCount = 10);

/// <summary>
/// Query to get cases that have breached SLA.
/// </summary>
public sealed record GetSlaBreachedCasesQuery(
    Guid? StoreId = null,
    string? BreachType = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int PageNumber = 1,
    int PageSize = 20);
