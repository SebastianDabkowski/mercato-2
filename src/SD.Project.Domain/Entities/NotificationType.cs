namespace SD.Project.Domain.Entities;

/// <summary>
/// Types of notifications that can be sent to users.
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Notifications related to order events (placed, shipped, delivered, etc.).
    /// </summary>
    OrderEvent,

    /// <summary>
    /// Notifications related to return requests and processing.
    /// </summary>
    Return,

    /// <summary>
    /// Notifications related to seller payouts.
    /// </summary>
    Payout,

    /// <summary>
    /// Notifications for messages (case messages, support, etc.).
    /// </summary>
    Message,

    /// <summary>
    /// System-wide announcements and updates.
    /// </summary>
    SystemUpdate,

    /// <summary>
    /// Notifications for product questions from buyers.
    /// </summary>
    ProductQuestion,

    /// <summary>
    /// Notifications for product question answers from sellers.
    /// </summary>
    ProductQuestionAnswer
}
