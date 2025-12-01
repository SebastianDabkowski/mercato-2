using Microsoft.Extensions.Logging;
using SD.Project.Application.Interfaces;

namespace SD.Project.Infrastructure.Services;

/// <summary>
/// Notification service that sends emails via IEmailSender and logs all notification events.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private const string DefaultCarrierName = "Unknown Carrier";
    private readonly ILogger<NotificationService> _logger;
    private readonly IEmailSender _emailSender;

    public NotificationService(ILogger<NotificationService> logger, IEmailSender emailSender)
    {
        _logger = logger;
        _emailSender = emailSender;
    }

    public Task SendProductCreatedAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        // Product notifications are logged only (no email recipients specified)
        _logger.LogInformation("Product {ProductId} created", productId);
        return Task.CompletedTask;
    }

    public Task SendProductUpdatedAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        // Product notifications are logged only (no email recipients specified)
        _logger.LogInformation("Product {ProductId} updated", productId);
        return Task.CompletedTask;
    }

    public Task SendProductDeletedAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        // Product notifications are logged only (no email recipients specified)
        _logger.LogInformation("Product {ProductId} deleted (archived)", productId);
        return Task.CompletedTask;
    }

    public Task SendProductStatusChangedAsync(Guid productId, string previousStatus, string newStatus, CancellationToken cancellationToken = default)
    {
        // Product notifications are logged only (no email recipients specified)
        _logger.LogInformation("Product {ProductId} status changed from {PreviousStatus} to {NewStatus}", productId, previousStatus, newStatus);
        return Task.CompletedTask;
    }

    public async Task SendRegistrationConfirmationAsync(Guid userId, string email, string firstName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending registration confirmation email to {Email} for user {UserId}",
            email, userId);

        var message = new EmailMessage(
            To: email,
            Subject: "Welcome to Mercato Marketplace!",
            HtmlBody: $@"
                <html>
                <body>
                    <h1>Welcome, {firstName}!</h1>
                    <p>Thank you for registering with Mercato Marketplace.</p>
                    <p>Your account has been created successfully. You can now start exploring our marketplace.</p>
                    <p>If you have any questions, please don't hesitate to contact our support team.</p>
                    <p>Best regards,<br/>The Mercato Team</p>
                </body>
                </html>",
            TextBody: $"Welcome, {firstName}! Thank you for registering with Mercato Marketplace. Your account has been created successfully.",
            TemplateName: "RegistrationConfirmation",
            Locale: "en-US");

        await _emailSender.SendAsync(message, cancellationToken);
    }

    public async Task SendEmailVerificationAsync(Guid userId, string email, string verificationToken, CancellationToken cancellationToken = default)
    {
        var verificationLink = $"/VerifyEmail?token={verificationToken}";
        _logger.LogInformation(
            "Sending verification email to {Email} for user {UserId}",
            email, userId);

        var message = new EmailMessage(
            To: email,
            Subject: "Verify Your Email Address - Mercato Marketplace",
            HtmlBody: $@"
                <html>
                <body>
                    <h1>Verify Your Email Address</h1>
                    <p>Please click the link below to verify your email address:</p>
                    <p><a href='{verificationLink}'>Verify Email</a></p>
                    <p>If you didn't create an account with us, please ignore this email.</p>
                    <p>Best regards,<br/>The Mercato Team</p>
                </body>
                </html>",
            TextBody: $"Please verify your email address by visiting: {verificationLink}",
            TemplateName: "EmailVerification",
            Locale: "en-US");

        await _emailSender.SendAsync(message, cancellationToken);
    }

    public async Task SendPasswordResetEmailAsync(Guid userId, string email, string resetToken, CancellationToken cancellationToken = default)
    {
        var resetLink = $"/ResetPassword?token={resetToken}";
        _logger.LogInformation(
            "Sending password reset email to {Email} for user {UserId}",
            email, userId);

        var message = new EmailMessage(
            To: email,
            Subject: "Reset Your Password - Mercato Marketplace",
            HtmlBody: $@"
                <html>
                <body>
                    <h1>Reset Your Password</h1>
                    <p>You requested to reset your password. Click the link below to create a new password:</p>
                    <p><a href='{resetLink}'>Reset Password</a></p>
                    <p>This link will expire in 24 hours.</p>
                    <p>If you didn't request this, please ignore this email.</p>
                    <p>Best regards,<br/>The Mercato Team</p>
                </body>
                </html>",
            TextBody: $"Reset your password by visiting: {resetLink}",
            TemplateName: "PasswordReset",
            Locale: "en-US");

        await _emailSender.SendAsync(message, cancellationToken);
    }

    public async Task SendInternalUserInvitationAsync(string email, string storeName, string role, string invitationToken, CancellationToken cancellationToken = default)
    {
        var invitationLink = $"/AcceptInvitation?token={invitationToken}";
        _logger.LogInformation(
            "Sending internal user invitation to {Email} for store {StoreName} with role {Role}",
            email, storeName, role);

        var message = new EmailMessage(
            To: email,
            Subject: $"You're Invited to Join {storeName} on Mercato Marketplace",
            HtmlBody: $@"
                <html>
                <body>
                    <h1>You're Invited!</h1>
                    <p>You have been invited to join <strong>{storeName}</strong> on Mercato Marketplace as a <strong>{role}</strong>.</p>
                    <p><a href='{invitationLink}'>Accept Invitation</a></p>
                    <p>This invitation will expire in 7 days.</p>
                    <p>Best regards,<br/>The Mercato Team</p>
                </body>
                </html>",
            TextBody: $"You have been invited to join {storeName} as a {role}. Accept at: {invitationLink}",
            TemplateName: "InternalUserInvitation",
            Locale: "en-US");

        await _emailSender.SendAsync(message, cancellationToken);
    }

    public Task SendBulkUpdateCompletedAsync(Guid sellerId, int successCount, int failureCount, CancellationToken cancellationToken = default)
    {
        // Bulk update notifications are logged only (seller doesn't have email in this context)
        _logger.LogInformation(
            "Bulk update completed for seller {SellerId}: {SuccessCount} succeeded, {FailureCount} failed",
            sellerId, successCount, failureCount);
        return Task.CompletedTask;
    }

    public async Task SendOrderConfirmationAsync(
        Guid orderId,
        string buyerEmail,
        string orderNumber,
        decimal totalAmount,
        string currency,
        CancellationToken cancellationToken = default)
    {
        var confirmationLink = $"/Buyer/OrderConfirmation/{orderId}";
        _logger.LogInformation(
            "Sending order confirmation email to {BuyerEmail} for order {OrderNumber}",
            buyerEmail, orderNumber);

        var message = new EmailMessage(
            To: buyerEmail,
            Subject: $"Order Confirmation - {orderNumber}",
            HtmlBody: $@"
                <html>
                <body>
                    <h1>Thank You for Your Order!</h1>
                    <p>Your order <strong>{orderNumber}</strong> has been confirmed.</p>
                    <p>Total: <strong>{currency} {totalAmount:N2}</strong></p>
                    <p><a href='{confirmationLink}'>View Order Details</a></p>
                    <p>We'll send you another email when your order ships.</p>
                    <p>Best regards,<br/>The Mercato Team</p>
                </body>
                </html>",
            TextBody: $"Thank you for your order {orderNumber}. Total: {currency} {totalAmount:N2}. View details at: {confirmationLink}",
            TemplateName: "OrderConfirmation",
            Locale: "en-US");

        await _emailSender.SendAsync(message, cancellationToken);
    }

    public async Task SendShipmentStatusChangedAsync(
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
        var trackingInfo = !string.IsNullOrEmpty(trackingNumber)
            ? $"Tracking: {carrierName ?? DefaultCarrierName} - {trackingNumber}"
            : "";
        _logger.LogInformation(
            "Sending shipment status notification to {BuyerEmail} for order {OrderNumber}. Status: {NewStatus}",
            buyerEmail, orderNumber, newStatus);

        var subject = newStatus.Equals("Shipped", StringComparison.OrdinalIgnoreCase)
            ? $"Your Order {orderNumber} Has Shipped!"
            : $"Order {orderNumber} - Shipment Update";

        var trackingHtml = !string.IsNullOrEmpty(trackingNumber)
            ? $"<p>Carrier: <strong>{carrierName ?? DefaultCarrierName}</strong><br/>Tracking Number: <strong>{trackingNumber}</strong></p>"
            : "";

        var message = new EmailMessage(
            To: buyerEmail,
            Subject: subject,
            HtmlBody: $@"
                <html>
                <body>
                    <h1>Shipment Update</h1>
                    <p>Your order <strong>{orderNumber}</strong> has been updated.</p>
                    <p>Status: <strong>{newStatus}</strong></p>
                    {trackingHtml}
                    <p>Best regards,<br/>The Mercato Team</p>
                </body>
                </html>",
            TextBody: $"Your order {orderNumber} status: {newStatus}. {trackingInfo}",
            TemplateName: "ShipmentStatusChanged",
            Locale: "en-US");

        await _emailSender.SendAsync(message, cancellationToken);
    }

    public async Task SendTrackingInfoUpdatedAsync(
        Guid shipmentId,
        Guid orderId,
        string buyerEmail,
        string orderNumber,
        string? trackingNumber,
        string? carrierName,
        string? trackingUrl,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending tracking info update to {BuyerEmail} for order {OrderNumber}",
            buyerEmail, orderNumber);

        var trackingLinkHtml = !string.IsNullOrEmpty(trackingUrl)
            ? $"<p><a href='{trackingUrl}'>Track Your Package</a></p>"
            : "";

        var message = new EmailMessage(
            To: buyerEmail,
            Subject: $"Tracking Update for Order {orderNumber}",
            HtmlBody: $@"
                <html>
                <body>
                    <h1>Tracking Information Updated</h1>
                    <p>Tracking has been updated for your order <strong>{orderNumber}</strong>.</p>
                    <p>Carrier: <strong>{carrierName ?? DefaultCarrierName}</strong></p>
                    <p>Tracking Number: <strong>{trackingNumber ?? "Pending"}</strong></p>
                    {trackingLinkHtml}
                    <p>Best regards,<br/>The Mercato Team</p>
                </body>
                </html>",
            TextBody: $"Tracking update for order {orderNumber}. Carrier: {carrierName ?? DefaultCarrierName}, Tracking: {trackingNumber ?? "Pending"}",
            TemplateName: "TrackingInfoUpdated",
            Locale: "en-US");

        await _emailSender.SendAsync(message, cancellationToken);
    }

    public async Task SendReturnRequestCreatedAsync(
        Guid returnRequestId,
        string orderNumber,
        string sellerEmail,
        string reason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending return request notification to seller {SellerEmail} for order {OrderNumber}",
            sellerEmail, orderNumber);

        var message = new EmailMessage(
            To: sellerEmail,
            Subject: $"Return Request Received - Order {orderNumber}",
            HtmlBody: $@"
                <html>
                <body>
                    <h1>Return Request Received</h1>
                    <p>A buyer has requested a return for order <strong>{orderNumber}</strong>.</p>
                    <p>Reason: {reason}</p>
                    <p>Please review this request in your seller dashboard.</p>
                    <p>Best regards,<br/>The Mercato Team</p>
                </body>
                </html>",
            TextBody: $"Return request for order {orderNumber}. Reason: {reason}",
            TemplateName: "ReturnRequestCreated",
            Locale: "en-US");

        await _emailSender.SendAsync(message, cancellationToken);
    }

    public async Task SendReturnRequestApprovedAsync(
        Guid returnRequestId,
        string orderNumber,
        string buyerEmail,
        string? sellerResponse,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending return approved notification to {BuyerEmail} for order {OrderNumber}",
            buyerEmail, orderNumber);

        var responseHtml = !string.IsNullOrEmpty(sellerResponse)
            ? $"<p>Seller's message: {sellerResponse}</p>"
            : "";

        var message = new EmailMessage(
            To: buyerEmail,
            Subject: $"Return Request Approved - Order {orderNumber}",
            HtmlBody: $@"
                <html>
                <body>
                    <h1>Return Request Approved</h1>
                    <p>Good news! Your return request for order <strong>{orderNumber}</strong> has been approved.</p>
                    {responseHtml}
                    <p>Please follow the return instructions provided in your account.</p>
                    <p>Best regards,<br/>The Mercato Team</p>
                </body>
                </html>",
            TextBody: $"Your return request for order {orderNumber} has been approved.",
            TemplateName: "ReturnRequestApproved",
            Locale: "en-US");

        await _emailSender.SendAsync(message, cancellationToken);
    }

    public async Task SendReturnRequestRejectedAsync(
        Guid returnRequestId,
        string orderNumber,
        string buyerEmail,
        string rejectionReason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending return rejected notification to {BuyerEmail} for order {OrderNumber}",
            buyerEmail, orderNumber);

        var message = new EmailMessage(
            To: buyerEmail,
            Subject: $"Return Request Update - Order {orderNumber}",
            HtmlBody: $@"
                <html>
                <body>
                    <h1>Return Request Update</h1>
                    <p>Your return request for order <strong>{orderNumber}</strong> could not be approved.</p>
                    <p>Reason: {rejectionReason}</p>
                    <p>If you have questions, please contact our support team.</p>
                    <p>Best regards,<br/>The Mercato Team</p>
                </body>
                </html>",
            TextBody: $"Your return request for order {orderNumber} was not approved. Reason: {rejectionReason}",
            TemplateName: "ReturnRequestRejected",
            Locale: "en-US");

        await _emailSender.SendAsync(message, cancellationToken);
    }

    public async Task SendReturnRequestCompletedAsync(
        Guid returnRequestId,
        string orderNumber,
        string buyerEmail,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending return completed notification to {BuyerEmail} for order {OrderNumber}",
            buyerEmail, orderNumber);

        var message = new EmailMessage(
            To: buyerEmail,
            Subject: $"Return Completed - Order {orderNumber}",
            HtmlBody: $@"
                <html>
                <body>
                    <h1>Return Completed</h1>
                    <p>Your return for order <strong>{orderNumber}</strong> has been completed.</p>
                    <p>Your refund will be processed shortly.</p>
                    <p>Thank you for shopping with Mercato.</p>
                    <p>Best regards,<br/>The Mercato Team</p>
                </body>
                </html>",
            TextBody: $"Your return for order {orderNumber} has been completed. Refund will be processed shortly.",
            TemplateName: "ReturnRequestCompleted",
            Locale: "en-US");

        await _emailSender.SendAsync(message, cancellationToken);
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
        // Item-level notifications are logged for now (Phase 2: partial fulfilment)
        var trackingInfo = !string.IsNullOrEmpty(trackingNumber)
            ? $" Tracking: {carrierName ?? DefaultCarrierName} - {trackingNumber}"
            : "";
        _logger.LogInformation(
            "Item status changed notification for {BuyerEmail}, order {OrderNumber}. " +
            "Product: {ProductName}. Status: {NewStatus}.{TrackingInfo}",
            buyerEmail, orderNumber, productName, newStatus, trackingInfo);
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
        // Batch item notifications are logged for now (Phase 2: partial fulfilment)
        _logger.LogInformation(
            "Batch item status changed notification for {BuyerEmail}, order {OrderNumber}. " +
            "{ItemCount} items changed to status {NewStatus}",
            buyerEmail, orderNumber, itemCount, newStatus);
        return Task.CompletedTask;
    }

    public async Task SendItemsRefundedAsync(
        Guid orderId,
        string buyerEmail,
        string orderNumber,
        int itemCount,
        decimal refundAmount,
        string currency,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending items refunded notification to {BuyerEmail} for order {OrderNumber}",
            buyerEmail, orderNumber);

        var message = new EmailMessage(
            To: buyerEmail,
            Subject: $"Refund Processed - Order {orderNumber}",
            HtmlBody: $@"
                <html>
                <body>
                    <h1>Refund Processed</h1>
                    <p>A refund has been processed for {itemCount} item(s) from your order <strong>{orderNumber}</strong>.</p>
                    <p>Refund Amount: <strong>{currency} {refundAmount:N2}</strong></p>
                    <p>The refund will be credited to your original payment method within 5-10 business days.</p>
                    <p>Best regards,<br/>The Mercato Team</p>
                </body>
                </html>",
            TextBody: $"Refund of {currency} {refundAmount:N2} processed for {itemCount} item(s) from order {orderNumber}.",
            TemplateName: "ItemsRefunded",
            Locale: "en-US");

        await _emailSender.SendAsync(message, cancellationToken);
    }

    public async Task SendPaymentFailedAsync(
        Guid orderId,
        string buyerEmail,
        string orderNumber,
        decimal totalAmount,
        string currency,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending payment failed notification to {BuyerEmail} for order {OrderNumber}",
            buyerEmail, orderNumber);

        var message = new EmailMessage(
            To: buyerEmail,
            Subject: $"Payment Failed - Order {orderNumber}",
            HtmlBody: $@"
                <html>
                <body>
                    <h1>Payment Failed</h1>
                    <p>We were unable to process your payment for order <strong>{orderNumber}</strong>.</p>
                    <p>Amount: <strong>{currency} {totalAmount:N2}</strong></p>
                    <p>Please update your payment method or try again.</p>
                    <p>Best regards,<br/>The Mercato Team</p>
                </body>
                </html>",
            TextBody: $"Payment failed for order {orderNumber}. Amount: {currency} {totalAmount:N2}. Please try again.",
            TemplateName: "PaymentFailed",
            Locale: "en-US");

        await _emailSender.SendAsync(message, cancellationToken);
    }

    public async Task SendRefundProcessedAsync(
        Guid orderId,
        string buyerEmail,
        string orderNumber,
        decimal refundAmount,
        string currency,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending refund processed notification to {BuyerEmail} for order {OrderNumber}",
            buyerEmail, orderNumber);

        var message = new EmailMessage(
            To: buyerEmail,
            Subject: $"Refund Processed - Order {orderNumber}",
            HtmlBody: $@"
                <html>
                <body>
                    <h1>Refund Processed</h1>
                    <p>A refund has been processed for your order <strong>{orderNumber}</strong>.</p>
                    <p>Refund Amount: <strong>{currency} {refundAmount:N2}</strong></p>
                    <p>The refund will be credited to your original payment method within 5-10 business days.</p>
                    <p>Best regards,<br/>The Mercato Team</p>
                </body>
                </html>",
            TextBody: $"Refund of {currency} {refundAmount:N2} processed for order {orderNumber}.",
            TemplateName: "RefundProcessed",
            Locale: "en-US");

        await _emailSender.SendAsync(message, cancellationToken);
    }

    public Task SendPayoutScheduledNotificationAsync(
        Guid sellerId,
        Guid payoutId,
        decimal amount,
        string currency,
        DateTime scheduledDate,
        CancellationToken cancellationToken = default)
    {
        // TODO: Replace logging with real email/notification integration.
        // Notify seller when a payout has been scheduled.
        _logger.LogInformation(
            "Payout scheduled notification sent to seller {SellerId}. " +
            "Payout ID: {PayoutId}. Amount: {Currency} {Amount:N2}. Scheduled date: {ScheduledDate:yyyy-MM-dd}",
            sellerId,
            payoutId,
            currency,
            amount,
            scheduledDate);
        return Task.CompletedTask;
    }

    public Task SendPayoutCompletedNotificationAsync(
        Guid sellerId,
        Guid payoutId,
        decimal amount,
        string currency,
        string? payoutReference,
        CancellationToken cancellationToken = default)
    {
        // TODO: Replace logging with real email/notification integration.
        // Notify seller when a payout has been successfully completed.
        _logger.LogInformation(
            "Payout completed notification sent to seller {SellerId}. " +
            "Payout ID: {PayoutId}. Amount: {Currency} {Amount:N2}. Reference: {PayoutReference}",
            sellerId,
            payoutId,
            currency,
            amount,
            payoutReference ?? "N/A");
        return Task.CompletedTask;
    }

    public Task SendPayoutFailedNotificationAsync(
        Guid sellerId,
        Guid payoutId,
        decimal amount,
        string currency,
        string? errorMessage,
        bool canRetry,
        CancellationToken cancellationToken = default)
    {
        // TODO: Replace logging with real email/notification integration.
        // Notify seller when a payout has failed and whether retry is possible.
        var retryInfo = canRetry ? "Retry will be attempted automatically." : "Maximum retries exceeded.";
        _logger.LogInformation(
            "Payout failed notification sent to seller {SellerId}. " +
            "Payout ID: {PayoutId}. Amount: {Currency} {Amount:N2}. Error: {ErrorMessage}. {RetryInfo}",
            sellerId,
            payoutId,
            currency,
            amount,
            errorMessage ?? "Unknown error",
            retryInfo);
        return Task.CompletedTask;
    }

    public Task SendSettlementGeneratedNotificationAsync(
        Guid sellerId,
        Guid settlementId,
        string settlementNumber,
        decimal netPayable,
        string currency,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        // TODO: Replace logging with real email/notification integration.
        // Notify seller when a monthly settlement has been generated.
        _logger.LogInformation(
            "Settlement generated notification sent to seller {SellerId}. " +
            "Settlement ID: {SettlementId}. Number: {SettlementNumber}. " +
            "Period: {Year}-{Month:D2}. Net payable: {Currency} {NetPayable:N2}",
            sellerId,
            settlementId,
            settlementNumber,
            year,
            month,
            currency,
            netPayable);
        return Task.CompletedTask;
    }

    public Task SendCommissionInvoiceIssuedAsync(
        Guid sellerId,
        Guid invoiceId,
        string invoiceNumber,
        decimal grossAmount,
        string currency,
        DateTime dueDate,
        CancellationToken cancellationToken = default)
    {
        // TODO: Replace logging with real email/notification integration.
        // Notify seller when a commission invoice has been issued.
        _logger.LogInformation(
            "Commission invoice issued notification sent to seller {SellerId}. " +
            "Invoice ID: {InvoiceId}. Number: {InvoiceNumber}. " +
            "Amount: {Currency} {GrossAmount:N2}. Due date: {DueDate:yyyy-MM-dd}",
            sellerId,
            invoiceId,
            invoiceNumber,
            currency,
            grossAmount,
            dueDate);
        return Task.CompletedTask;
    }

    public Task SendRefundProviderErrorAsync(
        Guid refundId,
        Guid orderId,
        string orderNumber,
        decimal refundAmount,
        string currency,
        string? errorMessage,
        string? errorCode,
        Guid initiatorId,
        string initiatorType,
        bool canRetry,
        CancellationToken cancellationToken = default)
    {
        // Refund provider errors are logged only (support team notification - no buyer email)
        var retryInfo = canRetry ? "Retry is available." : "Maximum retries exceeded.";
        _logger.LogWarning(
            "Refund provider error. Refund ID: {RefundId}, Order: {OrderNumber}. " +
            "Amount: {Currency} {RefundAmount:N2}. Error: {ErrorMessage} ({ErrorCode}). {RetryInfo}",
            refundId, orderNumber, currency, refundAmount,
            errorMessage ?? "Unknown error", errorCode ?? "N/A", retryInfo);
        return Task.CompletedTask;
    }

    public async Task SendPartialRefundProcessedAsync(
        Guid orderId,
        string buyerEmail,
        string orderNumber,
        decimal refundAmount,
        decimal remainingAmount,
        string currency,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending partial refund notification to {BuyerEmail} for order {OrderNumber}",
            buyerEmail, orderNumber);

        var message = new EmailMessage(
            To: buyerEmail,
            Subject: $"Partial Refund Processed - Order {orderNumber}",
            HtmlBody: $@"
                <html>
                <body>
                    <h1>Partial Refund Processed</h1>
                    <p>A partial refund has been processed for your order <strong>{orderNumber}</strong>.</p>
                    <p>Refund Amount: <strong>{currency} {refundAmount:N2}</strong></p>
                    <p>Remaining Order Balance: <strong>{currency} {remainingAmount:N2}</strong></p>
                    <p>The refund will be credited to your original payment method within 5-10 business days.</p>
                    <p>Best regards,<br/>The Mercato Team</p>
                </body>
                </html>",
            TextBody: $"Partial refund of {currency} {refundAmount:N2} processed for order {orderNumber}. Remaining balance: {currency} {remainingAmount:N2}",
            TemplateName: "PartialRefundProcessed",
            Locale: "en-US");

        await _emailSender.SendAsync(message, cancellationToken);
    }

    public async Task SendCaseMessageReceivedAsync(
        Guid returnRequestId,
        string caseNumber,
        string recipientEmail,
        string senderName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending case message notification to {RecipientEmail} for case {CaseNumber}",
            recipientEmail, caseNumber);

        var message = new EmailMessage(
            To: recipientEmail,
            Subject: $"New Message - Case {caseNumber}",
            HtmlBody: $@"
                <html>
                <body>
                    <h1>New Message in Your Case</h1>
                    <p>You have received a new message from <strong>{senderName}</strong> regarding case <strong>{caseNumber}</strong>.</p>
                    <p>Please log in to your account to view and respond to the message.</p>
                    <p>Best regards,<br/>The Mercato Team</p>
                </body>
                </html>",
            TextBody: $"New message from {senderName} in case {caseNumber}. Log in to view and respond.",
            TemplateName: "CaseMessageReceived",
            Locale: "en-US");

        await _emailSender.SendAsync(message, cancellationToken);
    }

    public async Task SendCaseResolvedAsync(
        Guid returnRequestId,
        string caseNumber,
        string orderNumber,
        string buyerEmail,
        string resolutionType,
        string? resolutionNotes,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending case resolved notification to {BuyerEmail} for case {CaseNumber}",
            buyerEmail, caseNumber);

        var notesHtml = !string.IsNullOrEmpty(resolutionNotes)
            ? $"<p>Notes: {resolutionNotes}</p>"
            : "";

        var message = new EmailMessage(
            To: buyerEmail,
            Subject: $"Case Resolved - {caseNumber}",
            HtmlBody: $@"
                <html>
                <body>
                    <h1>Your Case Has Been Resolved</h1>
                    <p>Your case <strong>{caseNumber}</strong> for order <strong>{orderNumber}</strong> has been resolved.</p>
                    <p>Resolution: <strong>{resolutionType}</strong></p>
                    {notesHtml}
                    <p>Thank you for your patience.</p>
                    <p>Best regards,<br/>The Mercato Team</p>
                </body>
                </html>",
            TextBody: $"Case {caseNumber} for order {orderNumber} resolved. Resolution: {resolutionType}",
            TemplateName: "CaseResolved",
            Locale: "en-US");

        await _emailSender.SendAsync(message, cancellationToken);
    }

    public async Task SendCaseEscalatedAsync(
        Guid returnRequestId,
        string caseNumber,
        string orderNumber,
        string buyerEmail,
        string sellerEmail,
        string escalationReason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending case escalation notifications for case {CaseNumber}",
            caseNumber);

        // Send to buyer
        var buyerMessage = new EmailMessage(
            To: buyerEmail,
            Subject: $"Case Escalated for Review - {caseNumber}",
            HtmlBody: $@"
                <html>
                <body>
                    <h1>Case Under Review</h1>
                    <p>Your case <strong>{caseNumber}</strong> for order <strong>{orderNumber}</strong> has been escalated for admin review.</p>
                    <p>Our team will review the case and make a decision. You will be notified of the outcome.</p>
                    <p>Best regards,<br/>The Mercato Team</p>
                </body>
                </html>",
            TextBody: $"Case {caseNumber} for order {orderNumber} has been escalated for admin review.",
            TemplateName: "CaseEscalatedBuyer",
            Locale: "en-US");

        await _emailSender.SendAsync(buyerMessage, cancellationToken);

        // Send to seller
        var sellerMessage = new EmailMessage(
            To: sellerEmail,
            Subject: $"Case Escalated for Review - {caseNumber}",
            HtmlBody: $@"
                <html>
                <body>
                    <h1>Case Under Admin Review</h1>
                    <p>Case <strong>{caseNumber}</strong> for order <strong>{orderNumber}</strong> has been escalated for admin review.</p>
                    <p>Reason: {escalationReason}</p>
                    <p>Our team will review the case and make a decision.</p>
                    <p>Best regards,<br/>The Mercato Team</p>
                </body>
                </html>",
            TextBody: $"Case {caseNumber} for order {orderNumber} has been escalated. Reason: {escalationReason}",
            TemplateName: "CaseEscalatedSeller",
            Locale: "en-US");

        await _emailSender.SendAsync(sellerMessage, cancellationToken);
    }

    public async Task SendAdminDecisionRecordedAsync(
        Guid returnRequestId,
        string caseNumber,
        string orderNumber,
        string buyerEmail,
        string sellerEmail,
        string decisionType,
        string? decisionNotes,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending admin decision notifications for case {CaseNumber}",
            caseNumber);

        var notesHtml = !string.IsNullOrEmpty(decisionNotes)
            ? $"<p>Notes: {decisionNotes}</p>"
            : "";

        // Send to buyer
        var buyerMessage = new EmailMessage(
            To: buyerEmail,
            Subject: $"Case Decision - {caseNumber}",
            HtmlBody: $@"
                <html>
                <body>
                    <h1>Case Decision</h1>
                    <p>A decision has been made regarding case <strong>{caseNumber}</strong> for order <strong>{orderNumber}</strong>.</p>
                    <p>Decision: <strong>{decisionType}</strong></p>
                    {notesHtml}
                    <p>Best regards,<br/>The Mercato Team</p>
                </body>
                </html>",
            TextBody: $"Decision made for case {caseNumber}. Decision: {decisionType}",
            TemplateName: "AdminDecisionBuyer",
            Locale: "en-US");

        await _emailSender.SendAsync(buyerMessage, cancellationToken);

        // Send to seller
        var sellerMessage = new EmailMessage(
            To: sellerEmail,
            Subject: $"Case Decision - {caseNumber}",
            HtmlBody: $@"
                <html>
                <body>
                    <h1>Case Decision</h1>
                    <p>A decision has been made regarding case <strong>{caseNumber}</strong> for order <strong>{orderNumber}</strong>.</p>
                    <p>Decision: <strong>{decisionType}</strong></p>
                    {notesHtml}
                    <p>Best regards,<br/>The Mercato Team</p>
                </body>
                </html>",
            TextBody: $"Decision made for case {caseNumber}. Decision: {decisionType}",
            TemplateName: "AdminDecisionSeller",
            Locale: "en-US");

        await _emailSender.SendAsync(sellerMessage, cancellationToken);
    }

    public async Task SendSlaBreachNotificationAsync(
        Guid returnRequestId,
        string caseNumber,
        string orderNumber,
        string sellerEmail,
        string breachType,
        DateTime deadline,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "Sending SLA breach notification to seller {SellerEmail} for case {CaseNumber}",
            sellerEmail, caseNumber);

        var message = new EmailMessage(
            To: sellerEmail,
            Subject: $"[URGENT] SLA Breach - Case {caseNumber}",
            HtmlBody: $@"
                <html>
                <body>
                    <h1 style='color: #d9534f;'>SLA Breach Alert</h1>
                    <p>Case <strong>{caseNumber}</strong> for order <strong>{orderNumber}</strong> has breached SLA.</p>
                    <p>Breach Type: <strong>{breachType}</strong></p>
                    <p>Deadline: <strong>{deadline:yyyy-MM-dd HH:mm:ss}</strong></p>
                    <p>Please take immediate action to resolve this case.</p>
                    <p>Best regards,<br/>The Mercato Team</p>
                </body>
                </html>",
            TextBody: $"URGENT: SLA breach for case {caseNumber}. Breach type: {breachType}. Deadline was: {deadline:yyyy-MM-dd HH:mm:ss}",
            TemplateName: "SlaBreach",
            Locale: "en-US");

        await _emailSender.SendAsync(message, cancellationToken);
    }

    public async Task SendSlaWarningNotificationAsync(
        Guid returnRequestId,
        string caseNumber,
        string orderNumber,
        string sellerEmail,
        string deadlineType,
        DateTime deadline,
        int hoursRemaining,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending SLA warning notification to seller {SellerEmail} for case {CaseNumber}. Hours remaining: {HoursRemaining}",
            sellerEmail, caseNumber, hoursRemaining);

        var message = new EmailMessage(
            To: sellerEmail,
            Subject: $"[Warning] Case {caseNumber} - Action Required",
            HtmlBody: $@"
                <html>
                <body>
                    <h1 style='color: #f0ad4e;'>SLA Warning</h1>
                    <p>Case <strong>{caseNumber}</strong> for order <strong>{orderNumber}</strong> is approaching its SLA deadline.</p>
                    <p>Deadline Type: <strong>{deadlineType}</strong></p>
                    <p>Deadline: <strong>{deadline:yyyy-MM-dd HH:mm:ss}</strong></p>
                    <p>Time Remaining: <strong>{hoursRemaining} hours</strong></p>
                    <p>Please respond to this case promptly to avoid an SLA breach.</p>
                    <p>Best regards,<br/>The Mercato Team</p>
                </body>
                </html>",
            TextBody: $"Warning: Case {caseNumber} deadline approaching. {hoursRemaining} hours remaining. Deadline: {deadline:yyyy-MM-dd HH:mm:ss}",
            TemplateName: "SlaWarning",
            Locale: "en-US");

        await _emailSender.SendAsync(message, cancellationToken);
    }
}
