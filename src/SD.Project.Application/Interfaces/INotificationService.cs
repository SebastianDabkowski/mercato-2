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
    /// Sends a registration confirmation email to a newly registered buyer.
    /// This is sent after successful registration to confirm the account was created.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="email">The email address to send to.</param>
    /// <param name="firstName">The user's first name for personalization.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendRegistrationConfirmationAsync(Guid userId, string email, string firstName, CancellationToken cancellationToken = default);

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
    /// Sends a notification to the seller when a new order is placed for their products.
    /// </summary>
    /// <param name="orderId">The ID of the order.</param>
    /// <param name="shipmentId">The ID of the seller's shipment within the order.</param>
    /// <param name="sellerEmail">The seller's email address.</param>
    /// <param name="orderNumber">The order number for display.</param>
    /// <param name="itemCount">Number of items in this seller's portion of the order.</param>
    /// <param name="subtotal">The subtotal for this seller's items.</param>
    /// <param name="currency">The currency code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendNewOrderNotificationToSellerAsync(
        Guid orderId,
        Guid shipmentId,
        string sellerEmail,
        string orderNumber,
        int itemCount,
        decimal subtotal,
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

    /// <summary>
    /// Sends a notification when tracking information is updated for a shipment.
    /// </summary>
    /// <param name="shipmentId">The ID of the shipment.</param>
    /// <param name="orderId">The ID of the parent order.</param>
    /// <param name="buyerEmail">The buyer's email address.</param>
    /// <param name="orderNumber">The order number for display.</param>
    /// <param name="trackingNumber">The tracking number.</param>
    /// <param name="carrierName">The carrier name.</param>
    /// <param name="trackingUrl">Optional tracking URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendTrackingInfoUpdatedAsync(
        Guid shipmentId,
        Guid orderId,
        string buyerEmail,
        string orderNumber,
        string? trackingNumber,
        string? carrierName,
        string? trackingUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to the seller when a return request is created.
    /// </summary>
    /// <param name="returnRequestId">The ID of the return request.</param>
    /// <param name="orderNumber">The order number for display.</param>
    /// <param name="sellerEmail">The seller's email address.</param>
    /// <param name="reason">The reason for the return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendReturnRequestCreatedAsync(
        Guid returnRequestId,
        string orderNumber,
        string sellerEmail,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to the buyer when a return request is approved.
    /// </summary>
    /// <param name="returnRequestId">The ID of the return request.</param>
    /// <param name="orderNumber">The order number for display.</param>
    /// <param name="buyerEmail">The buyer's email address.</param>
    /// <param name="sellerResponse">Optional response message from the seller.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendReturnRequestApprovedAsync(
        Guid returnRequestId,
        string orderNumber,
        string buyerEmail,
        string? sellerResponse,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to the buyer when a return request is rejected.
    /// </summary>
    /// <param name="returnRequestId">The ID of the return request.</param>
    /// <param name="orderNumber">The order number for display.</param>
    /// <param name="buyerEmail">The buyer's email address.</param>
    /// <param name="rejectionReason">The reason for rejection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendReturnRequestRejectedAsync(
        Guid returnRequestId,
        string orderNumber,
        string buyerEmail,
        string rejectionReason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to the buyer when a return request is completed.
    /// </summary>
    /// <param name="returnRequestId">The ID of the return request.</param>
    /// <param name="orderNumber">The order number for display.</param>
    /// <param name="buyerEmail">The buyer's email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendReturnRequestCompletedAsync(
        Guid returnRequestId,
        string orderNumber,
        string buyerEmail,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification when an individual item's status changes (Phase 2: partial fulfilment).
    /// </summary>
    /// <param name="itemId">The ID of the order item.</param>
    /// <param name="orderId">The ID of the parent order.</param>
    /// <param name="buyerEmail">The buyer's email address.</param>
    /// <param name="orderNumber">The order number for display.</param>
    /// <param name="productName">The name of the product.</param>
    /// <param name="previousStatus">The previous status.</param>
    /// <param name="newStatus">The new status.</param>
    /// <param name="trackingNumber">Optional tracking number if shipped.</param>
    /// <param name="carrierName">Optional carrier name if shipped.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendItemStatusChangedAsync(
        Guid itemId,
        Guid orderId,
        string buyerEmail,
        string orderNumber,
        string productName,
        string previousStatus,
        string newStatus,
        string? trackingNumber,
        string? carrierName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification when multiple items' status changes at once (Phase 2: partial fulfilment).
    /// </summary>
    /// <param name="orderId">The ID of the parent order.</param>
    /// <param name="buyerEmail">The buyer's email address.</param>
    /// <param name="orderNumber">The order number for display.</param>
    /// <param name="itemCount">The number of items affected.</param>
    /// <param name="itemNames">Comma-separated list of product names.</param>
    /// <param name="newStatus">The new status for all items.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendBatchItemStatusChangedAsync(
        Guid orderId,
        string buyerEmail,
        string orderNumber,
        int itemCount,
        string itemNames,
        string newStatus,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification when items are refunded (Phase 2: partial fulfilment).
    /// </summary>
    /// <param name="orderId">The ID of the parent order.</param>
    /// <param name="buyerEmail">The buyer's email address.</param>
    /// <param name="orderNumber">The order number for display.</param>
    /// <param name="itemCount">The number of items refunded.</param>
    /// <param name="refundAmount">The total refund amount.</param>
    /// <param name="currency">The currency code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendItemsRefundedAsync(
        Guid orderId,
        string buyerEmail,
        string orderNumber,
        int itemCount,
        decimal refundAmount,
        string currency,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification when a payment fails.
    /// </summary>
    /// <param name="orderId">The ID of the order.</param>
    /// <param name="buyerEmail">The buyer's email address.</param>
    /// <param name="orderNumber">The order number for display.</param>
    /// <param name="totalAmount">The total order amount.</param>
    /// <param name="currency">The currency code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendPaymentFailedAsync(
        Guid orderId,
        string buyerEmail,
        string orderNumber,
        decimal totalAmount,
        string currency,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification when a refund is processed.
    /// </summary>
    /// <param name="orderId">The ID of the order.</param>
    /// <param name="buyerEmail">The buyer's email address.</param>
    /// <param name="orderNumber">The order number for display.</param>
    /// <param name="refundAmount">The refunded amount.</param>
    /// <param name="currency">The currency code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendRefundProcessedAsync(
        Guid orderId,
        string buyerEmail,
        string orderNumber,
        decimal refundAmount,
        string currency,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification when a payout is scheduled.
    /// </summary>
    /// <param name="sellerId">The seller's user ID.</param>
    /// <param name="sellerEmail">The seller's email address.</param>
    /// <param name="payoutId">The ID of the payout.</param>
    /// <param name="amount">The payout amount.</param>
    /// <param name="currency">The currency code.</param>
    /// <param name="scheduledDate">The scheduled payout date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendPayoutScheduledNotificationAsync(
        Guid sellerId,
        string sellerEmail,
        Guid payoutId,
        decimal amount,
        string currency,
        DateTime scheduledDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification when a payout is successfully completed.
    /// </summary>
    /// <param name="sellerId">The seller's user ID.</param>
    /// <param name="sellerEmail">The seller's email address.</param>
    /// <param name="payoutId">The ID of the payout.</param>
    /// <param name="amount">The payout amount.</param>
    /// <param name="currency">The currency code.</param>
    /// <param name="payoutReference">The payment provider reference.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendPayoutCompletedNotificationAsync(
        Guid sellerId,
        string sellerEmail,
        Guid payoutId,
        decimal amount,
        string currency,
        string? payoutReference,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification when a payout fails.
    /// </summary>
    /// <param name="sellerId">The seller's user ID.</param>
    /// <param name="sellerEmail">The seller's email address.</param>
    /// <param name="payoutId">The ID of the payout.</param>
    /// <param name="amount">The payout amount.</param>
    /// <param name="currency">The currency code.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="canRetry">Whether the payout can be retried.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendPayoutFailedNotificationAsync(
        Guid sellerId,
        string sellerEmail,
        Guid payoutId,
        decimal amount,
        string currency,
        string? errorMessage,
        bool canRetry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification when a monthly settlement is generated.
    /// </summary>
    /// <param name="sellerId">The seller's user ID.</param>
    /// <param name="settlementId">The ID of the settlement.</param>
    /// <param name="settlementNumber">The settlement reference number.</param>
    /// <param name="netPayable">The net amount payable.</param>
    /// <param name="currency">The currency code.</param>
    /// <param name="year">The settlement year.</param>
    /// <param name="month">The settlement month.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendSettlementGeneratedNotificationAsync(
        Guid sellerId,
        Guid settlementId,
        string settlementNumber,
        decimal netPayable,
        string currency,
        int year,
        int month,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification when a commission invoice is issued.
    /// </summary>
    /// <param name="sellerId">The seller's user ID.</param>
    /// <param name="invoiceId">The ID of the invoice.</param>
    /// <param name="invoiceNumber">The invoice number.</param>
    /// <param name="grossAmount">The gross amount of the invoice.</param>
    /// <param name="currency">The currency code.</param>
    /// <param name="dueDate">The payment due date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendCommissionInvoiceIssuedAsync(
        Guid sellerId,
        Guid invoiceId,
        string invoiceNumber,
        decimal grossAmount,
        string currency,
        DateTime dueDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification when a refund processing fails due to provider error.
    /// Used to notify support agents about failures that need attention.
    /// </summary>
    /// <param name="refundId">The ID of the failed refund.</param>
    /// <param name="orderId">The ID of the order.</param>
    /// <param name="orderNumber">The order number for display.</param>
    /// <param name="refundAmount">The refund amount.</param>
    /// <param name="currency">The currency code.</param>
    /// <param name="errorMessage">The error message from the provider.</param>
    /// <param name="errorCode">The error code from the provider.</param>
    /// <param name="initiatorId">The ID of the user who initiated the refund.</param>
    /// <param name="initiatorType">The type of initiator (SupportAgent, Seller).</param>
    /// <param name="canRetry">Whether the refund can be retried.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendRefundProviderErrorAsync(
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
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification when a partial refund is processed.
    /// </summary>
    /// <param name="orderId">The ID of the order.</param>
    /// <param name="buyerEmail">The buyer's email address.</param>
    /// <param name="orderNumber">The order number for display.</param>
    /// <param name="refundAmount">The refunded amount.</param>
    /// <param name="remainingAmount">The remaining order amount.</param>
    /// <param name="currency">The currency code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendPartialRefundProcessedAsync(
        Guid orderId,
        string buyerEmail,
        string orderNumber,
        decimal refundAmount,
        decimal remainingAmount,
        string currency,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification when a new message is received in a case thread.
    /// </summary>
    /// <param name="returnRequestId">The ID of the return request (case).</param>
    /// <param name="caseNumber">The case number for display.</param>
    /// <param name="recipientEmail">The recipient's email address.</param>
    /// <param name="senderName">The name of the sender.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendCaseMessageReceivedAsync(
        Guid returnRequestId,
        string caseNumber,
        string recipientEmail,
        string senderName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification when a case is resolved by the seller.
    /// </summary>
    /// <param name="returnRequestId">The ID of the return request (case).</param>
    /// <param name="caseNumber">The case number for display.</param>
    /// <param name="orderNumber">The order number for display.</param>
    /// <param name="buyerEmail">The buyer's email address.</param>
    /// <param name="resolutionType">The type of resolution (FullRefund, PartialRefund, etc.).</param>
    /// <param name="resolutionNotes">Optional notes from the seller about the resolution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendCaseResolvedAsync(
        Guid returnRequestId,
        string caseNumber,
        string orderNumber,
        string buyerEmail,
        string resolutionType,
        string? resolutionNotes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends notifications when a case is escalated to admin review.
    /// Notifies both the buyer and seller that the case is now under admin review.
    /// </summary>
    /// <param name="returnRequestId">The ID of the return request (case).</param>
    /// <param name="caseNumber">The case number for display.</param>
    /// <param name="orderNumber">The order number for display.</param>
    /// <param name="buyerEmail">The buyer's email address.</param>
    /// <param name="sellerEmail">The seller's email address.</param>
    /// <param name="escalationReason">The reason for escalation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendCaseEscalatedAsync(
        Guid returnRequestId,
        string caseNumber,
        string orderNumber,
        string buyerEmail,
        string sellerEmail,
        string escalationReason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends notifications when an admin records a decision on an escalated case.
    /// Notifies both the buyer and seller of the admin's decision.
    /// </summary>
    /// <param name="returnRequestId">The ID of the return request (case).</param>
    /// <param name="caseNumber">The case number for display.</param>
    /// <param name="orderNumber">The order number for display.</param>
    /// <param name="buyerEmail">The buyer's email address.</param>
    /// <param name="sellerEmail">The seller's email address.</param>
    /// <param name="decisionType">The type of admin decision.</param>
    /// <param name="decisionNotes">Notes explaining the decision.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendAdminDecisionRecordedAsync(
        Guid returnRequestId,
        string caseNumber,
        string orderNumber,
        string buyerEmail,
        string sellerEmail,
        string decisionType,
        string? decisionNotes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification when a case SLA is breached.
    /// Notifies the seller about the breach and surfaces it in admin views.
    /// </summary>
    /// <param name="returnRequestId">The ID of the return request (case).</param>
    /// <param name="caseNumber">The case number for display.</param>
    /// <param name="orderNumber">The order number for display.</param>
    /// <param name="sellerEmail">The seller's email address.</param>
    /// <param name="breachType">The type of SLA breach (FirstResponse or Resolution).</param>
    /// <param name="deadline">The deadline that was missed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendSlaBreachNotificationAsync(
        Guid returnRequestId,
        string caseNumber,
        string orderNumber,
        string sellerEmail,
        string breachType,
        DateTime deadline,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a warning notification when a case is approaching SLA breach.
    /// Used as a soft escalation to alert sellers before breaching.
    /// </summary>
    /// <param name="returnRequestId">The ID of the return request (case).</param>
    /// <param name="caseNumber">The case number for display.</param>
    /// <param name="orderNumber">The order number for display.</param>
    /// <param name="sellerEmail">The seller's email address.</param>
    /// <param name="deadlineType">The type of deadline approaching (FirstResponse or Resolution).</param>
    /// <param name="deadline">The deadline that is approaching.</param>
    /// <param name="hoursRemaining">Hours remaining until breach.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendSlaWarningNotificationAsync(
        Guid returnRequestId,
        string caseNumber,
        string orderNumber,
        string sellerEmail,
        string deadlineType,
        DateTime deadline,
        int hoursRemaining,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to the seller when a new product question is asked.
    /// </summary>
    /// <param name="questionId">The ID of the question.</param>
    /// <param name="productId">The ID of the product.</param>
    /// <param name="productName">The name of the product.</param>
    /// <param name="sellerEmail">The seller's email address.</param>
    /// <param name="buyerName">The display name of the buyer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendProductQuestionAskedAsync(
        Guid questionId,
        Guid productId,
        string productName,
        string sellerEmail,
        string buyerName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to the buyer when their product question is answered.
    /// </summary>
    /// <param name="questionId">The ID of the question.</param>
    /// <param name="productId">The ID of the product.</param>
    /// <param name="productName">The name of the product.</param>
    /// <param name="buyerEmail">The buyer's email address.</param>
    /// <param name="storeName">The name of the seller's store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendProductQuestionAnsweredAsync(
        Guid questionId,
        Guid productId,
        string productName,
        string buyerEmail,
        string storeName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification when a new message is received in an order thread.
    /// </summary>
    /// <param name="orderId">The ID of the order.</param>
    /// <param name="orderNumber">The order number for display.</param>
    /// <param name="recipientEmail">The recipient's email address.</param>
    /// <param name="senderName">The name of the sender.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendOrderMessageReceivedAsync(
        Guid orderId,
        string orderNumber,
        string recipientEmail,
        string senderName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification when a product is approved by a moderator.
    /// </summary>
    /// <param name="productId">The ID of the product.</param>
    /// <param name="productName">The name of the product.</param>
    /// <param name="sellerEmail">The seller's email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendProductApprovedAsync(
        Guid productId,
        string productName,
        string sellerEmail,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification when a product is rejected by a moderator.
    /// </summary>
    /// <param name="productId">The ID of the product.</param>
    /// <param name="productName">The name of the product.</param>
    /// <param name="sellerEmail">The seller's email address.</param>
    /// <param name="rejectionReason">The reason for rejection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendProductRejectedAsync(
        Guid productId,
        string productName,
        string sellerEmail,
        string rejectionReason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification when a product photo is removed by a moderator.
    /// </summary>
    /// <param name="photoId">The ID of the photo.</param>
    /// <param name="productId">The ID of the product.</param>
    /// <param name="productName">The name of the product.</param>
    /// <param name="sellerEmail">The seller's email address.</param>
    /// <param name="removalReason">The reason for removal.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendPhotoRemovedAsync(
        Guid photoId,
        Guid productId,
        string productName,
        string sellerEmail,
        string removalReason,
        CancellationToken cancellationToken = default);
}
