using Microsoft.Extensions.Logging;
using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Domain.Services;
using SD.Project.Domain.ValueObjects;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for processing payment webhooks from payment providers.
/// Handles status updates, refunds, and notifications triggered by payment events.
/// </summary>
public sealed class PaymentWebhookService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly INotificationService _notificationService;
    private readonly EscrowService _escrowService;
    private readonly ILogger<PaymentWebhookService> _logger;

    public PaymentWebhookService(
        IOrderRepository orderRepository,
        IUserRepository userRepository,
        IStoreRepository storeRepository,
        INotificationService notificationService,
        EscrowService escrowService,
        ILogger<PaymentWebhookService> logger)
    {
        _orderRepository = orderRepository;
        _userRepository = userRepository;
        _storeRepository = storeRepository;
        _notificationService = notificationService;
        _escrowService = escrowService;
        _logger = logger;
    }

    /// <summary>
    /// Processes a payment webhook from a payment provider.
    /// Maps the provider status to internal status and updates the order accordingly.
    /// </summary>
    public async Task<PaymentWebhookResultDto> HandleAsync(
        ProcessPaymentWebhookCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        _logger.LogInformation(
            "Processing payment webhook for order {OrderId}, transaction {TransactionId}, status {Status}, provider {Provider}",
            command.OrderId, command.TransactionId, command.ProviderStatusCode, command.ProviderName ?? "Unknown");

        // Get the order
        var order = await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            _logger.LogWarning("Order not found for webhook: {OrderId}", command.OrderId);
            return PaymentWebhookResultDto.Failed("Order not found.");
        }

        // Map provider status to internal payment status
        var newPaymentStatus = PaymentStatusMapper.MapFromProviderStatus(
            command.ProviderStatusCode,
            command.ProviderName);

        var previousPaymentStatus = order.PaymentStatus;

        // Check if status change is needed
        if (previousPaymentStatus == newPaymentStatus)
        {
            _logger.LogDebug(
                "No payment status change needed for order {OrderId}: {Status}",
                command.OrderId, newPaymentStatus);
            return PaymentWebhookResultDto.NoChange(previousPaymentStatus.ToString());
        }

        // Apply the status change based on the mapped payment status
        try
        {
            switch (newPaymentStatus)
            {
                case PaymentStatus.Paid:
                    if (order.Status == OrderStatus.Pending)
                    {
                        order.ConfirmPayment(command.TransactionId);
                        await _orderRepository.UpdateAsync(order, cancellationToken);
                        await _orderRepository.SaveChangesAsync(cancellationToken);

                        // Create escrow payment to hold funds
                        await _escrowService.CreateEscrowForOrderAsync(order, cancellationToken);

                        // Send order confirmation notification to buyer
                        await SendPaymentConfirmationAsync(order, cancellationToken);

                        // Send notifications to sellers for their sub-orders
                        await SendNewOrderNotificationsToSellersAsync(order, cancellationToken);
                    }
                    break;

                case PaymentStatus.Failed:
                    if (order.Status == OrderStatus.Pending)
                    {
                        order.FailPayment();
                        await _orderRepository.UpdateAsync(order, cancellationToken);
                        await _orderRepository.SaveChangesAsync(cancellationToken);

                        // Send payment failure notification
                        await SendPaymentFailureNotificationAsync(order, cancellationToken);
                    }
                    break;

                case PaymentStatus.Refunded:
                    // Handle refund - order can be refunded from various states
                    if (order.CanTransitionTo(OrderStatus.Refunded))
                    {
                        // Use the provided refund amount if available, otherwise full refund
                        order.Refund(command.RefundAmount);
                        await _orderRepository.UpdateAsync(order, cancellationToken);
                        await _orderRepository.SaveChangesAsync(cancellationToken);

                        // Send refund notification
                        await SendRefundNotificationAsync(order, cancellationToken);
                    }
                    break;

                case PaymentStatus.Pending:
                    // No action needed for pending status in webhook
                    _logger.LogDebug("Received pending status webhook for order {OrderId}", command.OrderId);
                    break;
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex,
                "Invalid status transition for order {OrderId}: {Message}",
                command.OrderId, ex.Message);
            return PaymentWebhookResultDto.Failed($"Invalid status transition: {ex.Message}");
        }

        _logger.LogInformation(
            "Payment status updated for order {OrderId}: {PreviousStatus} -> {NewStatus}",
            command.OrderId, previousPaymentStatus, newPaymentStatus);

        return PaymentWebhookResultDto.Succeeded(
            previousPaymentStatus.ToString(),
            newPaymentStatus.ToString());
    }

    /// <summary>
    /// Updates payment status based on a direct command (e.g., from admin or system).
    /// </summary>
    public async Task<PaymentWebhookResultDto> HandleAsync(
        UpdatePaymentStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Pass refund amount to the webhook command for proper handling
        var webhookCommand = new ProcessPaymentWebhookCommand(
            command.OrderId,
            command.TransactionId,
            command.ProviderStatusCode,
            command.ProviderName,
            null, // ProviderSignature
            null, // RawPayload
            command.RefundAmount);

        return await HandleAsync(webhookCommand, cancellationToken);
    }

    /// <summary>
    /// Creates a PaymentStatusDto from an order for display purposes.
    /// </summary>
    public static PaymentStatusDto CreatePaymentStatusDto(Order order)
    {
        var status = order.PaymentStatus;
        var displayMessage = PaymentStatusMapper.GetBuyerFriendlyMessage(status);
        
        string? failureMessage = null;
        if (status == PaymentStatus.Failed)
        {
            failureMessage = PaymentStatusMapper.GetFailureMessageForBuyer();
        }

        return new PaymentStatusDto(
            Status: status.ToString(),
            DisplayMessage: displayMessage,
            IsPaid: status == PaymentStatus.Paid,
            IsFailed: status == PaymentStatus.Failed,
            IsRefunded: status == PaymentStatus.Refunded,
            IsPending: status == PaymentStatus.Pending,
            RefundedAmount: order.RefundedAmount,
            FailureMessage: failureMessage);
    }

    private async Task SendPaymentConfirmationAsync(Order order, CancellationToken cancellationToken)
    {
        var buyer = await _userRepository.GetByIdAsync(order.BuyerId, cancellationToken);
        if (buyer?.Email is not null)
        {
            await _notificationService.SendOrderConfirmationAsync(
                order.Id,
                buyer.Email.Value,
                order.OrderNumber,
                order.TotalAmount,
                order.Currency,
                cancellationToken);
        }
    }

    private async Task SendPaymentFailureNotificationAsync(Order order, CancellationToken cancellationToken)
    {
        var buyer = await _userRepository.GetByIdAsync(order.BuyerId, cancellationToken);
        if (buyer?.Email is not null)
        {
            await _notificationService.SendPaymentFailedAsync(
                order.Id,
                buyer.Email.Value,
                order.OrderNumber,
                order.TotalAmount,
                order.Currency,
                cancellationToken);
        }
    }

    private async Task SendRefundNotificationAsync(Order order, CancellationToken cancellationToken)
    {
        var buyer = await _userRepository.GetByIdAsync(order.BuyerId, cancellationToken);
        if (buyer?.Email is not null)
        {
            await _notificationService.SendRefundProcessedAsync(
                order.Id,
                buyer.Email.Value,
                order.OrderNumber,
                order.RefundedAmount ?? order.TotalAmount,
                order.Currency,
                cancellationToken);
        }
    }

    /// <summary>
    /// Sends new order notifications to all sellers who have items in the order.
    /// </summary>
    private async Task SendNewOrderNotificationsToSellersAsync(Order order, CancellationToken cancellationToken)
    {
        // Get all store IDs from the order shipments
        var storeIds = order.Shipments.Select(s => s.StoreId).Distinct().ToList();
        var stores = await _storeRepository.GetByIdsAsync(storeIds, cancellationToken);

        foreach (var shipment in order.Shipments)
        {
            var store = stores.FirstOrDefault(s => s.Id == shipment.StoreId);
            if (store is null)
            {
                continue;
            }

            // Get the seller for this store
            var seller = await _userRepository.GetByIdAsync(store.SellerId, cancellationToken);
            if (seller?.Email is null)
            {
                continue;
            }

            // Count items for this seller
            var sellerItems = order.Items.Where(i => i.StoreId == shipment.StoreId).ToList();
            var itemCount = sellerItems.Sum(i => i.Quantity);

            await _notificationService.SendNewOrderNotificationToSellerAsync(
                order.Id,
                shipment.Id,
                seller.Email.Value,
                order.OrderNumber,
                itemCount,
                shipment.Subtotal,
                order.Currency,
                cancellationToken);
        }
    }
}
