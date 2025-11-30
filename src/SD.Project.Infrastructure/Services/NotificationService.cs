using Microsoft.Extensions.Logging;
using SD.Project.Application.Interfaces;

namespace SD.Project.Infrastructure.Services;

/// <summary>
/// Logs notification intents until real channel is available.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private const string DefaultCarrierName = "Unknown Carrier";
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendProductCreatedAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        // TODO: Replace logging with integration (email, message bus, etc.).
        _logger.LogInformation("Product {ProductId} created", productId);
        return Task.CompletedTask;
    }

    public Task SendProductUpdatedAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        // TODO: Replace logging with integration (email, message bus, etc.).
        _logger.LogInformation("Product {ProductId} updated", productId);
        return Task.CompletedTask;
    }

    public Task SendProductDeletedAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        // TODO: Replace logging with integration (email, message bus, etc.).
        _logger.LogInformation("Product {ProductId} deleted (archived)", productId);
        return Task.CompletedTask;
    }

    public Task SendProductStatusChangedAsync(Guid productId, string previousStatus, string newStatus, CancellationToken cancellationToken = default)
    {
        // TODO: Replace logging with integration (email, message bus, etc.).
        _logger.LogInformation("Product {ProductId} status changed from {PreviousStatus} to {NewStatus}", productId, previousStatus, newStatus);
        return Task.CompletedTask;
    }

    public Task SendEmailVerificationAsync(Guid userId, string email, string verificationToken, CancellationToken cancellationToken = default)
    {
        // TODO: Replace logging with real email sending integration.
        // In a production environment, this would generate a full URL like:
        // https://yourdomain.com/VerifyEmail?token={verificationToken}
        var verificationLink = $"/VerifyEmail?token={verificationToken}";
        _logger.LogInformation(
            "Verification email sent to {Email} for user {UserId}. Verification link: {VerificationLink}",
            email, userId, verificationLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(Guid userId, string email, string resetToken, CancellationToken cancellationToken = default)
    {
        // TODO: Replace logging with real email sending integration.
        // In a production environment, this would generate a full URL like:
        // https://yourdomain.com/ResetPassword?token={resetToken}
        var resetLink = $"/ResetPassword?token={resetToken}";
        _logger.LogInformation(
            "Password reset email sent to {Email} for user {UserId}. Reset link: {ResetLink}",
            email, userId, resetLink);
        return Task.CompletedTask;
    }

    public Task SendInternalUserInvitationAsync(string email, string storeName, string role, string invitationToken, CancellationToken cancellationToken = default)
    {
        // TODO: Replace logging with real email sending integration.
        // In a production environment, this would generate a full URL like:
        // https://yourdomain.com/AcceptInvitation?token={invitationToken}
        var invitationLink = $"/AcceptInvitation?token={invitationToken}";
        _logger.LogInformation(
            "Internal user invitation sent to {Email} for store {StoreName} with role {Role}. Invitation link: {InvitationLink}",
            email, storeName, role, invitationLink);
        return Task.CompletedTask;
    }

    public Task SendBulkUpdateCompletedAsync(Guid sellerId, int successCount, int failureCount, CancellationToken cancellationToken = default)
    {
        // TODO: Replace logging with real notification integration.
        _logger.LogInformation(
            "Bulk update completed for seller {SellerId}: {SuccessCount} succeeded, {FailureCount} failed",
            sellerId, successCount, failureCount);
        return Task.CompletedTask;
    }

    public Task SendOrderConfirmationAsync(
        Guid orderId,
        string buyerEmail,
        string orderNumber,
        decimal totalAmount,
        string currency,
        CancellationToken cancellationToken = default)
    {
        // TODO: Replace logging with real email/notification integration.
        // In a production environment, this would send an email with order details,
        // tracking information, and receipt to the buyer using an email template.
        // Email templates should be configurable and localized.
        var confirmationLink = $"/Buyer/OrderConfirmation/{orderId}";
        _logger.LogInformation(
            "Order confirmation email sent to {BuyerEmail} for order {OrderNumber}. " +
            "Total: {Currency} {TotalAmount:N2}. Confirmation link: {ConfirmationLink}",
            buyerEmail,
            orderNumber,
            currency,
            totalAmount,
            confirmationLink);
        return Task.CompletedTask;
    }

    public Task SendShipmentStatusChangedAsync(
        Guid shipmentId,
        Guid orderId,
        string buyerEmail,
        string orderNumber,
        string previousStatus,
        string newStatus,
        string? trackingNumber,
        string? carrierName,
        CancellationToken cancellationToken = default)
    {
        // TODO: Replace logging with real email/notification integration.
        // In a production environment, this would send an email notification to the buyer
        // when the shipment status changes (e.g., preparing, shipped, delivered).
        // For shipped status, include tracking information if available.
        var trackingInfo = !string.IsNullOrEmpty(trackingNumber)
            ? $" Tracking: {carrierName ?? DefaultCarrierName} - {trackingNumber}"
            : "";
        _logger.LogInformation(
            "Shipment status changed notification sent to {BuyerEmail} for order {OrderNumber}. " +
            "Status: {PreviousStatus} -> {NewStatus}.{TrackingInfo}",
            buyerEmail,
            orderNumber,
            previousStatus,
            newStatus,
            trackingInfo);
        return Task.CompletedTask;
    }

    public Task SendReturnRequestCreatedAsync(
        Guid returnRequestId,
        string orderNumber,
        string sellerEmail,
        string reason,
        CancellationToken cancellationToken = default)
    {
        // TODO: Replace logging with real email/notification integration.
        _logger.LogInformation(
            "Return request notification sent to seller {SellerEmail} for order {OrderNumber}. " +
            "Return request ID: {ReturnRequestId}. Reason: {Reason}",
            sellerEmail,
            orderNumber,
            returnRequestId,
            reason);
        return Task.CompletedTask;
    }

    public Task SendReturnRequestApprovedAsync(
        Guid returnRequestId,
        string orderNumber,
        string buyerEmail,
        string? sellerResponse,
        CancellationToken cancellationToken = default)
    {
        // TODO: Replace logging with real email/notification integration.
        var responseInfo = !string.IsNullOrEmpty(sellerResponse)
            ? $" Seller response: {sellerResponse}"
            : "";
        _logger.LogInformation(
            "Return request approved notification sent to buyer {BuyerEmail} for order {OrderNumber}. " +
            "Return request ID: {ReturnRequestId}.{ResponseInfo}",
            buyerEmail,
            orderNumber,
            returnRequestId,
            responseInfo);
        return Task.CompletedTask;
    }

    public Task SendReturnRequestRejectedAsync(
        Guid returnRequestId,
        string orderNumber,
        string buyerEmail,
        string rejectionReason,
        CancellationToken cancellationToken = default)
    {
        // TODO: Replace logging with real email/notification integration.
        _logger.LogInformation(
            "Return request rejected notification sent to buyer {BuyerEmail} for order {OrderNumber}. " +
            "Return request ID: {ReturnRequestId}. Rejection reason: {RejectionReason}",
            buyerEmail,
            orderNumber,
            returnRequestId,
            rejectionReason);
        return Task.CompletedTask;
    }

    public Task SendReturnRequestCompletedAsync(
        Guid returnRequestId,
        string orderNumber,
        string buyerEmail,
        CancellationToken cancellationToken = default)
    {
        // TODO: Replace logging with real email/notification integration.
        _logger.LogInformation(
            "Return request completed notification sent to buyer {BuyerEmail} for order {OrderNumber}. " +
            "Return request ID: {ReturnRequestId}",
            buyerEmail,
            orderNumber,
            returnRequestId);
        return Task.CompletedTask;
    }

    public Task SendItemStatusChangedAsync(
        Guid itemId,
        Guid orderId,
        string buyerEmail,
        string orderNumber,
        string productName,
        string previousStatus,
        string newStatus,
        string? trackingNumber,
        string? carrierName,
        CancellationToken cancellationToken = default)
    {
        // TODO: Replace logging with real email/notification integration.
        // Phase 2: Partial fulfilment - notify buyer when individual item status changes.
        var trackingInfo = !string.IsNullOrEmpty(trackingNumber)
            ? $" Tracking: {carrierName ?? DefaultCarrierName} - {trackingNumber}"
            : "";
        _logger.LogInformation(
            "Item status changed notification sent to {BuyerEmail} for order {OrderNumber}. " +
            "Product: {ProductName}. Status: {PreviousStatus} -> {NewStatus}.{TrackingInfo}",
            buyerEmail,
            orderNumber,
            productName,
            previousStatus,
            newStatus,
            trackingInfo);
        return Task.CompletedTask;
    }

    public Task SendBatchItemStatusChangedAsync(
        Guid orderId,
        string buyerEmail,
        string orderNumber,
        int itemCount,
        string itemNames,
        string newStatus,
        CancellationToken cancellationToken = default)
    {
        // TODO: Replace logging with real email/notification integration.
        // Phase 2: Partial fulfilment - notify buyer when multiple items' status changes.
        _logger.LogInformation(
            "Batch item status changed notification sent to {BuyerEmail} for order {OrderNumber}. " +
            "{ItemCount} items ({ItemNames}) changed to status {NewStatus}",
            buyerEmail,
            orderNumber,
            itemCount,
            itemNames,
            newStatus);
        return Task.CompletedTask;
    }

    public Task SendItemsRefundedAsync(
        Guid orderId,
        string buyerEmail,
        string orderNumber,
        int itemCount,
        decimal refundAmount,
        string currency,
        CancellationToken cancellationToken = default)
    {
        // TODO: Replace logging with real email/notification integration.
        // Phase 2: Partial fulfilment - notify buyer when items are refunded.
        _logger.LogInformation(
            "Items refunded notification sent to {BuyerEmail} for order {OrderNumber}. " +
            "{ItemCount} items refunded for {Currency} {RefundAmount:N2}",
            buyerEmail,
            orderNumber,
            itemCount,
            currency,
            refundAmount);
        return Task.CompletedTask;
    }

    public Task SendPaymentFailedAsync(
        Guid orderId,
        string buyerEmail,
        string orderNumber,
        decimal totalAmount,
        string currency,
        CancellationToken cancellationToken = default)
    {
        // TODO: Replace logging with real email/notification integration.
        // Notify buyer when payment fails so they can retry or use a different payment method.
        _logger.LogInformation(
            "Payment failed notification sent to {BuyerEmail} for order {OrderNumber}. " +
            "Amount: {Currency} {TotalAmount:N2}",
            buyerEmail,
            orderNumber,
            currency,
            totalAmount);
        return Task.CompletedTask;
    }

    public Task SendRefundProcessedAsync(
        Guid orderId,
        string buyerEmail,
        string orderNumber,
        decimal refundAmount,
        string currency,
        CancellationToken cancellationToken = default)
    {
        // TODO: Replace logging with real email/notification integration.
        // Notify buyer when a refund has been processed for their order.
        _logger.LogInformation(
            "Refund processed notification sent to {BuyerEmail} for order {OrderNumber}. " +
            "Refund amount: {Currency} {RefundAmount:N2}",
            buyerEmail,
            orderNumber,
            currency,
            refundAmount);
        return Task.CompletedTask;
    }
}
