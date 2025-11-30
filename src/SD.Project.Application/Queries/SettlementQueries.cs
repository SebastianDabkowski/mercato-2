using SD.Project.Domain.Entities;

namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get a settlement by ID.
/// </summary>
public sealed record GetSettlementByIdQuery(Guid SettlementId);

/// <summary>
/// Query to get settlement details with items and adjustments.
/// </summary>
public sealed record GetSettlementDetailsQuery(Guid SettlementId);

/// <summary>
/// Query to get settlements for a store with pagination.
/// </summary>
public sealed record GetSettlementsByStoreIdQuery(
    Guid StoreId,
    int Skip = 0,
    int Take = 20);

/// <summary>
/// Query to get settlements for admin view with filtering and pagination.
/// </summary>
public sealed record GetSettlementsQuery(
    Guid? StoreId = null,
    int? Year = null,
    int? Month = null,
    SettlementStatus? Status = null,
    int PageNumber = 1,
    int PageSize = 20);

/// <summary>
/// Query to get settlements summary for a period.
/// </summary>
public sealed record GetSettlementsSummaryQuery(
    int? Year = null,
    int? Month = null);

/// <summary>
/// Query to get settlement export data.
/// </summary>
public sealed record GetSettlementExportQuery(Guid SettlementId);

/// <summary>
/// Query to get settlement items export data.
/// </summary>
public sealed record GetSettlementItemsExportQuery(Guid SettlementId);
