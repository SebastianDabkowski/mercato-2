namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the role of a message sender in an order message thread.
/// </summary>
public enum OrderMessageSenderRole
{
    /// <summary>The message was sent by the buyer.</summary>
    Buyer,
    /// <summary>The message was sent by the seller.</summary>
    Seller,
    /// <summary>The message was sent by an admin for moderation/escalation.</summary>
    Admin
}

/// <summary>
/// Represents a private message between buyer and seller regarding an order.
/// Messages are only visible to the buyer, seller, and admin.
/// </summary>
public class OrderMessage
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The order this message is about.
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// The store associated with this message thread.
    /// For multi-seller orders, each seller has a separate thread.
    /// </summary>
    public Guid StoreId { get; private set; }

    /// <summary>
    /// The user who sent this message.
    /// </summary>
    public Guid SenderId { get; private set; }

    /// <summary>
    /// The role of the sender.
    /// </summary>
    public OrderMessageSenderRole SenderRole { get; private set; }

    /// <summary>
    /// The display name of the sender.
    /// </summary>
    public string SenderName { get; private set; } = default!;

    /// <summary>
    /// The message content.
    /// </summary>
    public string Content { get; private set; } = default!;

    /// <summary>
    /// When the message was sent.
    /// </summary>
    public DateTime SentAt { get; private set; }

    /// <summary>
    /// Whether this message has been read by the recipient.
    /// </summary>
    public bool IsRead { get; private set; }

    /// <summary>
    /// When the message was read (if read).
    /// </summary>
    public DateTime? ReadAt { get; private set; }

    /// <summary>
    /// Whether this message has been hidden by an admin.
    /// </summary>
    public bool IsHidden { get; private set; }

    /// <summary>
    /// Admin who hid the message (if hidden).
    /// </summary>
    public Guid? HiddenByAdminId { get; private set; }

    /// <summary>
    /// Reason for hiding the message.
    /// </summary>
    public string? HiddenReason { get; private set; }

    /// <summary>
    /// When the message was hidden.
    /// </summary>
    public DateTime? HiddenAt { get; private set; }

    private OrderMessage()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new order message.
    /// </summary>
    /// <param name="orderId">The ID of the order.</param>
    /// <param name="storeId">The ID of the store.</param>
    /// <param name="senderId">The ID of the sender.</param>
    /// <param name="senderRole">The role of the sender.</param>
    /// <param name="senderName">The display name of the sender.</param>
    /// <param name="content">The message content.</param>
    public OrderMessage(
        Guid orderId,
        Guid storeId,
        Guid senderId,
        OrderMessageSenderRole senderRole,
        string senderName,
        string content)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order ID is required.", nameof(orderId));
        }

        if (storeId == Guid.Empty)
        {
            throw new ArgumentException("Store ID is required.", nameof(storeId));
        }

        if (senderId == Guid.Empty)
        {
            throw new ArgumentException("Sender ID is required.", nameof(senderId));
        }

        if (string.IsNullOrWhiteSpace(senderName))
        {
            throw new ArgumentException("Sender name is required.", nameof(senderName));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Message content is required.", nameof(content));
        }

        if (content.Length > 5000)
        {
            throw new ArgumentException("Message content cannot exceed 5000 characters.", nameof(content));
        }

        Id = Guid.NewGuid();
        OrderId = orderId;
        StoreId = storeId;
        SenderId = senderId;
        SenderRole = senderRole;
        SenderName = senderName.Trim();
        Content = content.Trim();
        SentAt = DateTime.UtcNow;
        IsRead = false;
        IsHidden = false;
    }

    /// <summary>
    /// Marks the message as read.
    /// </summary>
    public void MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Hides the message (admin moderation).
    /// </summary>
    /// <param name="adminId">The ID of the admin hiding the message.</param>
    /// <param name="reason">The reason for hiding.</param>
    public void Hide(Guid adminId, string reason)
    {
        if (adminId == Guid.Empty)
        {
            throw new ArgumentException("Admin ID is required.", nameof(adminId));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reason is required.", nameof(reason));
        }

        IsHidden = true;
        HiddenByAdminId = adminId;
        HiddenReason = reason.Trim();
        HiddenAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Unhides the message (admin action).
    /// </summary>
    public void Unhide()
    {
        if (!IsHidden)
        {
            throw new InvalidOperationException("Message is not hidden.");
        }

        IsHidden = false;
        HiddenByAdminId = null;
        HiddenReason = null;
        HiddenAt = null;
    }
}
