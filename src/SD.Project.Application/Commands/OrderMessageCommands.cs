namespace SD.Project.Application.Commands;

/// <summary>
/// Command to send a message in an order thread.
/// </summary>
/// <param name="OrderId">The ID of the order.</param>
/// <param name="StoreId">The ID of the store in the thread.</param>
/// <param name="SenderId">The ID of the sender.</param>
/// <param name="SenderRole">The role of the sender (buyer, seller, admin).</param>
/// <param name="Content">The message content.</param>
public record SendOrderMessageCommand(
    Guid OrderId,
    Guid StoreId,
    Guid SenderId,
    string SenderRole,
    string Content);

/// <summary>
/// Command to mark messages as read in an order thread.
/// </summary>
/// <param name="OrderId">The ID of the order.</param>
/// <param name="StoreId">The ID of the store in the thread.</param>
/// <param name="UserId">The ID of the user marking as read.</param>
/// <param name="UserRole">The role of the user (buyer, seller, admin).</param>
public record MarkOrderMessagesReadCommand(
    Guid OrderId,
    Guid StoreId,
    Guid UserId,
    string UserRole);

/// <summary>
/// Command to hide an order message (admin moderation).
/// </summary>
/// <param name="MessageId">The ID of the message to hide.</param>
/// <param name="AdminId">The ID of the admin performing the action.</param>
/// <param name="Reason">The reason for hiding.</param>
public record HideOrderMessageCommand(
    Guid MessageId,
    Guid AdminId,
    string Reason);

/// <summary>
/// Command to unhide an order message (admin moderation).
/// </summary>
/// <param name="MessageId">The ID of the message to unhide.</param>
/// <param name="AdminId">The ID of the admin performing the action.</param>
public record UnhideOrderMessageCommand(
    Guid MessageId,
    Guid AdminId);
