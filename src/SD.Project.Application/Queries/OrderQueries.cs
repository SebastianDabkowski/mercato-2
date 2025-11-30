namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get order details for a buyer with seller sub-order breakdown.
/// </summary>
public sealed record GetBuyerOrderDetailsQuery(
    Guid BuyerId,
    Guid OrderId);

/// <summary>
/// Query to get seller's sub-orders (shipments) with pagination.
/// </summary>
public sealed record GetSellerSubOrdersQuery(
    Guid StoreId,
    int Skip = 0,
    int Take = 20);

/// <summary>
/// Query to get a specific seller sub-order details.
/// </summary>
public sealed record GetSellerSubOrderDetailsQuery(
    Guid StoreId,
    Guid SubOrderId);

/// <summary>
/// Query to get seller's sub-orders with filtering and pagination.
/// </summary>
public sealed record GetFilteredSellerSubOrdersQuery(
    Guid StoreId,
    string? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? BuyerSearch = null,
    int PageNumber = 1,
    int PageSize = 20);

/// <summary>
/// Query to export seller's sub-orders with optional filters.
/// </summary>
public sealed record ExportSellerSubOrdersQuery(
    Guid StoreId,
    ExportFormat Format,
    string? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? BuyerSearch = null);
