using SD.Project.Domain.Entities;

namespace SD.Project.Application.DTOs;

/// <summary>
/// Data transfer object for an order message.
/// </summary>
/// <param name="Id">The message ID.</param>
/// <param name="OrderId">The order ID.</param>
/// <param name="StoreId">The store ID.</param>
/// <param name="SenderId">The sender ID.</param>
/// <param name="SenderRole">The sender's role.</param>
/// <param name="SenderName">The sender's display name.</param>
/// <param name="Content">The message content.</param>
/// <param name="SentAt">When the message was sent.</param>
/// <param name="IsRead">Whether the message has been read.</param>
/// <param name="ReadAt">When the message was read (if read).</param>
public record OrderMessageDto(
    Guid Id,
    Guid OrderId,
    Guid StoreId,
    Guid SenderId,
    string SenderRole,
    string SenderName,
    string Content,
    DateTime SentAt,
    bool IsRead,
    DateTime? ReadAt);

/// <summary>
/// Data transfer object for an order message thread.
/// </summary>
/// <param name="OrderId">The order ID.</param>
/// <param name="OrderNumber">The order number.</param>
/// <param name="StoreId">The store ID.</param>
/// <param name="StoreName">The store name.</param>
/// <param name="Messages">The messages in the thread.</param>
/// <param name="UnreadCount">Number of unread messages.</param>
public record OrderMessageThreadDto(
    Guid OrderId,
    string OrderNumber,
    Guid StoreId,
    string StoreName,
    IReadOnlyList<OrderMessageDto> Messages,
    int UnreadCount);

/// <summary>
/// Summary of a message thread for listing purposes.
/// </summary>
/// <param name="OrderId">The order ID.</param>
/// <param name="OrderNumber">The order number.</param>
/// <param name="StoreId">The store ID.</param>
/// <param name="StoreName">The store name.</param>
/// <param name="LastMessagePreview">Preview of the last message.</param>
/// <param name="LastMessageAt">When the last message was sent.</param>
/// <param name="UnreadCount">Number of unread messages.</param>
public record OrderMessageThreadSummaryDto(
    Guid OrderId,
    string OrderNumber,
    Guid StoreId,
    string StoreName,
    string LastMessagePreview,
    DateTime LastMessageAt,
    int UnreadCount);

/// <summary>
/// Result of sending an order message.
/// </summary>
/// <param name="IsSuccess">Whether the operation succeeded.</param>
/// <param name="ErrorMessage">Error message if failed.</param>
/// <param name="MessageId">The created message ID if successful.</param>
public record SendOrderMessageResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    Guid? MessageId = null);

/// <summary>
/// Result of hiding or unhiding an order message.
/// </summary>
/// <param name="IsSuccess">Whether the operation succeeded.</param>
/// <param name="ErrorMessage">Error message if failed.</param>
public record ModerateOrderMessageResultDto(
    bool IsSuccess,
    string? ErrorMessage);
