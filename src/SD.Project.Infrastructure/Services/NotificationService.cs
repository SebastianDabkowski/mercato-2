using Microsoft.Extensions.Logging;
using SD.Project.Application.Interfaces;

namespace SD.Project.Infrastructure.Services;

/// <summary>
/// Logs notification intents until real channel is available.
/// </summary>
public sealed class NotificationService : INotificationService
{
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
}
