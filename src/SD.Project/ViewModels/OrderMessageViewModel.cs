namespace SD.Project.ViewModels;

/// <summary>
/// View model for displaying an order message.
/// </summary>
public record OrderMessageViewModel(
    Guid Id,
    Guid OrderId,
    Guid StoreId,
    string SenderRole,
    string SenderName,
    string Content,
    DateTime SentAt,
    bool IsRead)
{
    /// <summary>
    /// Gets the formatted date when the message was sent.
    /// </summary>
    public string SentAtDisplay => SentAt.ToString("MMM d, yyyy h:mm tt");

    /// <summary>
    /// Gets whether the message was sent by the buyer.
    /// </summary>
    public bool IsBuyerMessage => SenderRole.Equals("Buyer", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets whether the message was sent by the seller.
    /// </summary>
    public bool IsSellerMessage => SenderRole.Equals("Seller", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// View model for displaying an order message thread.
/// </summary>
public record OrderMessageThreadViewModel(
    Guid OrderId,
    string OrderNumber,
    Guid StoreId,
    string StoreName,
    IReadOnlyList<OrderMessageViewModel> Messages,
    int UnreadCount);

/// <summary>
/// View model for a message thread summary.
/// </summary>
public record OrderMessageThreadSummaryViewModel(
    Guid OrderId,
    string OrderNumber,
    Guid StoreId,
    string StoreName,
    string LastMessagePreview,
    DateTime LastMessageAt,
    int UnreadCount)
{
    /// <summary>
    /// Gets the formatted date of the last message.
    /// </summary>
    public string LastMessageAtDisplay => LastMessageAt.ToString("MMM d, yyyy");
}

/// <summary>
/// View model for the send message form input.
/// </summary>
public class SendOrderMessageInputModel
{
    public Guid OrderId { get; set; }
    public Guid StoreId { get; set; }
    public string Content { get; set; } = string.Empty;
}
