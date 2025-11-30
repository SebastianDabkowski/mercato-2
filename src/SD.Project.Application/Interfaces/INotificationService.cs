namespace SD.Project.Application.Interfaces;

/// <summary>
/// Abstraction for notifying users about changes.
/// </summary>
public interface INotificationService
{
    Task SendProductCreatedAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification when a product is updated.
    /// </summary>
    /// <param name="productId">The ID of the updated product.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendProductUpdatedAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification when a product is deleted (archived).
    /// </summary>
    /// <param name="productId">The ID of the deleted product.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendProductDeletedAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification when a product's workflow status changes.
    /// </summary>
    /// <param name="productId">The ID of the product.</param>
    /// <param name="previousStatus">The previous status.</param>
    /// <param name="newStatus">The new status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendProductStatusChangedAsync(Guid productId, string previousStatus, string newStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a verification email to a newly registered user with a verification link.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="email">The email address to send to.</param>
    /// <param name="verificationToken">The unique verification token to include in the link.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendEmailVerificationAsync(Guid userId, string email, string verificationToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a password reset email with a time-limited reset link.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="email">The email address to send to.</param>
    /// <param name="resetToken">The unique password reset token to include in the link.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendPasswordResetEmailAsync(Guid userId, string email, string resetToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an invitation email to a new internal user.
    /// </summary>
    /// <param name="email">The email address to send to.</param>
    /// <param name="storeName">The name of the store inviting the user.</param>
    /// <param name="role">The role being assigned to the user.</param>
    /// <param name="invitationToken">The unique invitation token to include in the link.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendInternalUserInvitationAsync(string email, string storeName, string role, string invitationToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a bulk update operation for audit purposes.
    /// </summary>
    /// <param name="sellerId">The ID of the seller performing the update.</param>
    /// <param name="successCount">Number of products successfully updated.</param>
    /// <param name="failureCount">Number of products that failed to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendBulkUpdateCompletedAsync(Guid sellerId, int successCount, int failureCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an order confirmation notification to the buyer.
    /// </summary>
    /// <param name="orderId">The ID of the order.</param>
    /// <param name="buyerEmail">The buyer's email address.</param>
    /// <param name="orderNumber">The order number for display.</param>
    /// <param name="totalAmount">The total order amount.</param>
    /// <param name="currency">The currency code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendOrderConfirmationAsync(
        Guid orderId,
        string buyerEmail,
        string orderNumber,
        decimal totalAmount,
        string currency,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification when a shipment status changes.
    /// </summary>
    /// <param name="shipmentId">The ID of the shipment.</param>
    /// <param name="orderId">The ID of the parent order.</param>
    /// <param name="buyerEmail">The buyer's email address.</param>
    /// <param name="orderNumber">The order number for display.</param>
    /// <param name="previousStatus">The previous status.</param>
    /// <param name="newStatus">The new status.</param>
    /// <param name="trackingNumber">Optional tracking number if shipped.</param>
    /// <param name="carrierName">Optional carrier name if shipped.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendShipmentStatusChangedAsync(
        Guid shipmentId,
        Guid orderId,
        string buyerEmail,
        string orderNumber,
        string previousStatus,
        string newStatus,
        string? trackingNumber,
        string? carrierName,
        CancellationToken cancellationToken = default);
}
