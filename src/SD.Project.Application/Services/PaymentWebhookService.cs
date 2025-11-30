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
    private readonly INotificationService _notificationService;
    private readonly EscrowService _escrowService;
    private readonly ILogger<PaymentWebhookService> _logger;

    public PaymentWebhookService(
        IOrderRepository orderRepository,
        IUserRepository userRepository,
        INotificationService notificationService,
        EscrowService escrowService,
        ILogger<PaymentWebhookService> logger)
    {
        _orderRepository = orderRepository;
        _userRepository = userRepository;
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

                        // Send order confirmation notification
                        await SendPaymentConfirmationAsync(order, cancellationToken);
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
                        order.Refund(); // Will use TotalAmount if no specific amount provided
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

        var webhookCommand = new ProcessPaymentWebhookCommand(
            command.OrderId,
            command.TransactionId,
            command.ProviderStatusCode,
            command.ProviderName);

        var result = await HandleAsync(webhookCommand, cancellationToken);

        // Handle specific refund amount if provided
        if (result.Success && command.RefundAmount.HasValue && command.RefundAmount.Value > 0)
        {
            var order = await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
            if (order is not null && order.Status == OrderStatus.Refunded && 
                order.RefundedAmount != command.RefundAmount)
            {
                // The refund amount was already set by the Refund() method
                // For partial refunds, we would need additional logic
                _logger.LogDebug(
                    "Refund amount for order {OrderId}: {Amount}",
                    command.OrderId, order.RefundedAmount);
            }
        }

        return result;
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
}
