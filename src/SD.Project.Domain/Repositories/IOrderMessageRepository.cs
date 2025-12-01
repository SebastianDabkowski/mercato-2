using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for order message persistence operations.
/// </summary>
public interface IOrderMessageRepository
{
    /// <summary>
    /// Gets a message by its ID.
    /// </summary>
    Task<OrderMessage?> GetByIdAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all messages for a specific order and store combination.
    /// </summary>
    Task<IReadOnlyList<OrderMessage>> GetMessagesForOrderAsync(
        Guid orderId,
        Guid storeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all message threads for a buyer (grouped by order/store).
    /// Returns the latest message from each thread.
    /// </summary>
    Task<IReadOnlyList<OrderMessage>> GetLatestMessagesForBuyerAsync(
        Guid buyerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all message threads for a store (grouped by order).
    /// Returns the latest message from each thread.
    /// </summary>
    Task<IReadOnlyList<OrderMessage>> GetLatestMessagesForStoreAsync(
        Guid storeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of unread messages for a buyer.
    /// </summary>
    Task<int> GetUnreadCountForBuyerAsync(Guid buyerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of unread messages for a store.
    /// </summary>
    Task<int> GetUnreadCountForStoreAsync(Guid storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all hidden messages for admin moderation view.
    /// </summary>
    Task<IReadOnlyList<OrderMessage>> GetHiddenMessagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new message.
    /// </summary>
    Task AddAsync(OrderMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing message.
    /// </summary>
    Task UpdateAsync(OrderMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks all messages in a thread as read for a specific recipient role.
    /// </summary>
    Task MarkAsReadAsync(
        Guid orderId,
        Guid storeId,
        OrderMessageSenderRole recipientRole,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
