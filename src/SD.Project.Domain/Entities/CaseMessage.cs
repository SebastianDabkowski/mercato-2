namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the role of a message sender in a case messaging thread.
/// </summary>
public enum CaseMessageSenderRole
{
    /// <summary>The message was sent by the buyer.</summary>
    Buyer,
    /// <summary>The message was sent by the seller.</summary>
    Seller,
    /// <summary>The message was sent by an admin for moderation/escalation.</summary>
    Admin
}

/// <summary>
/// Represents a message in a case (return/complaint) messaging thread.
/// Messages are text-only and form a chronological conversation between buyer and seller.
/// </summary>
public class CaseMessage
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The return request (case) this message belongs to.
    /// </summary>
    public Guid ReturnRequestId { get; private set; }

    /// <summary>
    /// The user who sent this message.
    /// </summary>
    public Guid SenderId { get; private set; }

    /// <summary>
    /// The role of the sender (Buyer, Seller, Admin).
    /// </summary>
    public CaseMessageSenderRole SenderRole { get; private set; }

    /// <summary>
    /// The name of the sender for display purposes.
    /// </summary>
    public string SenderName { get; private set; } = default!;

    /// <summary>
    /// The text content of the message.
    /// </summary>
    public string Content { get; private set; } = default!;

    /// <summary>
    /// When the message was sent.
    /// </summary>
    public DateTime SentAt { get; private set; }

    /// <summary>
    /// Whether this message has been read by the other party.
    /// </summary>
    public bool IsRead { get; private set; }

    /// <summary>
    /// When the message was read (if read).
    /// </summary>
    public DateTime? ReadAt { get; private set; }

    private CaseMessage()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new case message.
    /// </summary>
    /// <param name="returnRequestId">The ID of the return request (case).</param>
    /// <param name="senderId">The ID of the user sending the message.</param>
    /// <param name="senderRole">The role of the sender.</param>
    /// <param name="senderName">The display name of the sender.</param>
    /// <param name="content">The message content.</param>
    public CaseMessage(
        Guid returnRequestId,
        Guid senderId,
        CaseMessageSenderRole senderRole,
        string senderName,
        string content)
    {
        if (returnRequestId == Guid.Empty)
        {
            throw new ArgumentException("Return request ID is required.", nameof(returnRequestId));
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
        ReturnRequestId = returnRequestId;
        SenderId = senderId;
        SenderRole = senderRole;
        SenderName = senderName.Trim();
        Content = content.Trim();
        SentAt = DateTime.UtcNow;
        IsRead = false;
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
}
