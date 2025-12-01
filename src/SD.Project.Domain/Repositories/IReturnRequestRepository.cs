using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for return request persistence operations.
/// </summary>
public interface IReturnRequestRepository
{
    /// <summary>
    /// Gets a return request by ID.
    /// </summary>
    Task<ReturnRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a return request by ID with items loaded.
    /// </summary>
    Task<ReturnRequest?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a return request by case number.
    /// </summary>
    Task<ReturnRequest?> GetByCaseNumberAsync(string caseNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a return request by shipment ID.
    /// </summary>
    Task<ReturnRequest?> GetByShipmentIdAsync(Guid shipmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all return requests for a buyer.
    /// </summary>
    Task<IReadOnlyList<ReturnRequest>> GetByBuyerIdAsync(Guid buyerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all return requests for a store.
    /// </summary>
    Task<IReadOnlyList<ReturnRequest>> GetByStoreIdAsync(Guid storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets filtered and paginated return requests for a store.
    /// </summary>
    Task<(IReadOnlyList<ReturnRequest> Requests, int TotalCount)> GetFilteredByStoreIdAsync(
        Guid storeId,
        ReturnRequestStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        int skip,
        int take,
        ReturnRequestType? type = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets filtered and paginated return requests for admin view across all stores.
    /// </summary>
    Task<(IReadOnlyList<ReturnRequest> Requests, int TotalCount)> GetFilteredForAdminAsync(
        ReturnRequestStatus? status,
        ReturnRequestType? type,
        string? searchTerm,
        DateTime? fromDate,
        DateTime? toDate,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a return request already exists for the given shipment.
    /// </summary>
    Task<bool> ExistsForShipmentAsync(Guid shipmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an open (non-completed/rejected) return request exists for the given order item.
    /// Used to prevent duplicate requests for the same item.
    /// </summary>
    Task<bool> HasOpenRequestForOrderItemAsync(Guid orderItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets any open return requests that include the specified order item.
    /// </summary>
    Task<ReturnRequest?> GetOpenRequestForOrderItemAsync(Guid orderItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new return request.
    /// </summary>
    Task AddAsync(ReturnRequest returnRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing return request.
    /// </summary>
    Task UpdateAsync(ReturnRequest returnRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
