using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for case message persistence operations.
/// </summary>
public interface ICaseMessageRepository
{
    /// <summary>
    /// Gets all messages for a specific return request (case) in chronological order.
    /// </summary>
    Task<IReadOnlyList<CaseMessage>> GetByReturnRequestIdAsync(Guid returnRequestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of unread messages for a specific return request and recipient role.
    /// </summary>
    Task<int> GetUnreadCountAsync(Guid returnRequestId, CaseMessageSenderRole recipientRole, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of unread messages across all cases for a buyer.
    /// </summary>
    Task<int> GetUnreadCountForBuyerAsync(Guid buyerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of unread messages across all cases for a store.
    /// </summary>
    Task<int> GetUnreadCountForStoreAsync(Guid storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new message.
    /// </summary>
    Task AddAsync(CaseMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks all messages in a case as read for a specific recipient role.
    /// </summary>
    Task MarkAsReadAsync(Guid returnRequestId, CaseMessageSenderRole recipientRole, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
