using Microsoft.Extensions.Logging;
using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for managing refunds.
/// Orchestrates full and partial refund processing, integrates with payment providers,
/// and enforces business rules for seller-initiated refunds.
/// </summary>
public sealed class RefundService
{
    private readonly IRefundRepository _refundRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IEscrowRepository _escrowRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRefundProviderService _refundProviderService;
    private readonly INotificationService _notificationService;
    private readonly EscrowService _escrowService;
    private readonly ILogger<RefundService> _logger;

    // Business rule: Seller can only refund within these limits
    private const int SellerRefundWindowDays = 30;
    private const decimal SellerMaxRefundPercentage = 100m;

    public RefundService(
        IRefundRepository refundRepository,
        IOrderRepository orderRepository,
        IEscrowRepository escrowRepository,
        IStoreRepository storeRepository,
        IUserRepository userRepository,
        IRefundProviderService refundProviderService,
        INotificationService notificationService,
        EscrowService escrowService,
        ILogger<RefundService> logger)
    {
        _refundRepository = refundRepository;
        _orderRepository = orderRepository;
        _escrowRepository = escrowRepository;
        _storeRepository = storeRepository;
        _userRepository = userRepository;
        _refundProviderService = refundProviderService;
        _notificationService = notificationService;
        _escrowService = escrowService;
        _logger = logger;
    }

    /// <summary>
    /// Initiates a full refund for an order.
    /// Called by support agents to process complete refunds.
    /// </summary>
    public async Task<InitiateRefundResultDto> HandleAsync(
        InitiateFullRefundCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        _logger.LogInformation(
            "Initiating full refund for order {OrderId} by {InitiatorType} {InitiatedById}",
            command.OrderId, command.InitiatorType, command.InitiatedById);

        var order = await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            return InitiateRefundResultDto.Failed("Order not found.");
        }

        // Check if order can be refunded
        if (!order.CanTransitionTo(OrderStatus.Refunded))
        {
            return InitiateRefundResultDto.Failed($"Order in status {order.Status} cannot be refunded.");
        }

        // Check if already fully refunded
        var existingRefunds = await _refundRepository.GetByOrderIdAsync(command.OrderId, cancellationToken);
        var totalRefunded = existingRefunds
            .Where(r => r.Status == RefundStatus.Completed)
            .Sum(r => r.Amount);

        if (totalRefunded >= order.TotalAmount)
        {
            return InitiateRefundResultDto.Failed("Order has already been fully refunded.");
        }

        var remainingAmount = order.TotalAmount - totalRefunded;

        // Get escrow for commission calculation
        var escrow = await _escrowRepository.GetByOrderIdAsync(command.OrderId, cancellationToken);
        var totalCommissionRefund = escrow?.Allocations
            .Where(a => a.Status == EscrowAllocationStatus.Held)
            .Sum(a => a.CommissionAmount) ?? 0m;

        // Create refund record
        var refund = new Refund(
            order.Id,
            null,
            order.BuyerId,
            null,
            RefundType.Full,
            remainingAmount,
            order.Currency,
            totalCommissionRefund,
            command.Reason,
            order.PaymentTransactionId,
            command.InitiatedById,
            command.InitiatorType);

        await _refundRepository.AddAsync(refund, cancellationToken);
        await _refundRepository.SaveChangesAsync(cancellationToken);

        // Process the refund with payment provider
        var processResult = await ProcessRefundWithProviderAsync(refund, order, cancellationToken);

        return processResult.IsSuccess
            ? InitiateRefundResultDto.Succeeded(refund.Id, processResult.RefundTransactionId, processResult.Status)
            : InitiateRefundResultDto.Failed(processResult.ErrorMessage ?? "Failed to process refund.");
    }

    /// <summary>
    /// Initiates a partial refund for an order or specific shipment.
    /// </summary>
    public async Task<InitiateRefundResultDto> HandleAsync(
        InitiatePartialRefundCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        _logger.LogInformation(
            "Initiating partial refund of {Amount} for order {OrderId} (shipment: {ShipmentId}) by {InitiatorType} {InitiatedById}",
            command.Amount, command.OrderId, command.ShipmentId, command.InitiatorType, command.InitiatedById);

        var order = await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            return InitiateRefundResultDto.Failed("Order not found.");
        }

        // Validate refund amount
        var existingRefunds = await _refundRepository.GetByOrderIdAsync(command.OrderId, cancellationToken);
        var totalRefunded = existingRefunds
            .Where(r => r.Status == RefundStatus.Completed)
            .Sum(r => r.Amount);

        var remainingAmount = order.TotalAmount - totalRefunded;
        if (command.Amount > remainingAmount)
        {
            return InitiateRefundResultDto.Failed(
                $"Refund amount {command.Amount} exceeds remaining balance {remainingAmount}.");
        }

        if (command.Amount <= 0)
        {
            return InitiateRefundResultDto.Failed("Refund amount must be greater than zero.");
        }

        // Calculate proportional commission refund
        decimal commissionRefundAmount = 0m;
        Guid? storeId = null;

        if (command.ShipmentId.HasValue)
        {
            var allocation = await _escrowRepository.GetAllocationByShipmentIdAsync(
                command.ShipmentId.Value, cancellationToken);
            
            if (allocation is not null && allocation.Status == EscrowAllocationStatus.Held)
            {
                // Calculate proportional commission based on refund amount
                var refundRatio = command.Amount / allocation.TotalAmount;
                commissionRefundAmount = Math.Round(allocation.CommissionAmount * refundRatio, 2, MidpointRounding.ToEven);
                storeId = allocation.StoreId;
            }
        }
        else
        {
            // Order-level partial refund - calculate average commission ratio
            var escrow = await _escrowRepository.GetByOrderIdAsync(command.OrderId, cancellationToken);
            if (escrow is not null)
            {
                var heldAllocations = escrow.Allocations
                    .Where(a => a.Status == EscrowAllocationStatus.Held)
                    .ToList();
                
                if (heldAllocations.Count > 0)
                {
                    var totalHeld = heldAllocations.Sum(a => a.TotalAmount);
                    var totalCommission = heldAllocations.Sum(a => a.CommissionAmount);
                    var refundRatio = command.Amount / totalHeld;
                    commissionRefundAmount = Math.Round(totalCommission * refundRatio, 2, MidpointRounding.ToEven);
                }
            }
        }

        // Create refund record
        var refund = new Refund(
            order.Id,
            command.ShipmentId,
            order.BuyerId,
            storeId,
            RefundType.Partial,
            command.Amount,
            order.Currency,
            commissionRefundAmount,
            command.Reason,
            order.PaymentTransactionId,
            command.InitiatedById,
            command.InitiatorType);

        await _refundRepository.AddAsync(refund, cancellationToken);
        await _refundRepository.SaveChangesAsync(cancellationToken);

        // Process the refund with payment provider
        var processResult = await ProcessRefundWithProviderAsync(refund, order, cancellationToken);

        return processResult.IsSuccess
            ? InitiateRefundResultDto.Succeeded(refund.Id, processResult.RefundTransactionId, processResult.Status)
            : InitiateRefundResultDto.Failed(processResult.ErrorMessage ?? "Failed to process refund.");
    }

    /// <summary>
    /// Allows a seller to initiate a refund within business rules.
    /// </summary>
    public async Task<InitiateRefundResultDto> HandleAsync(
        SellerInitiateRefundCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        _logger.LogInformation(
            "Seller {SellerId} initiating refund for shipment {ShipmentId} (store: {StoreId})",
            command.SellerId, command.ShipmentId, command.StoreId);

        // Validate seller can initiate this refund
        var validationResult = await HandleAsync(
            new ValidateSellerRefundQuery(command.ShipmentId, command.StoreId, command.Amount),
            cancellationToken);

        if (!validationResult.IsAllowed)
        {
            return InitiateRefundResultDto.Failed(validationResult.ValidationMessage ?? "Refund not allowed.");
        }

        // Get shipment and order details
        var (shipment, order, _) = await _orderRepository.GetShipmentWithOrderAsync(
            command.ShipmentId, cancellationToken);

        if (shipment is null || order is null)
        {
            return InitiateRefundResultDto.Failed("Shipment not found.");
        }

        var refundAmount = command.Amount ?? (shipment.Subtotal + shipment.ShippingCost);

        // Calculate commission refund
        var allocation = await _escrowRepository.GetAllocationByShipmentIdAsync(
            command.ShipmentId, cancellationToken);
        
        decimal commissionRefundAmount = 0m;
        if (allocation is not null && allocation.Status == EscrowAllocationStatus.Held)
        {
            var refundRatio = refundAmount / allocation.TotalAmount;
            commissionRefundAmount = Math.Round(allocation.CommissionAmount * refundRatio, 2, MidpointRounding.ToEven);
        }

        var refundType = command.Amount.HasValue && command.Amount.Value < (shipment.Subtotal + shipment.ShippingCost)
            ? RefundType.Partial
            : RefundType.Full;

        // Create refund record
        var refund = new Refund(
            order.Id,
            command.ShipmentId,
            order.BuyerId,
            command.StoreId,
            refundType,
            refundAmount,
            order.Currency,
            commissionRefundAmount,
            command.Reason,
            order.PaymentTransactionId,
            command.SellerId,
            "Seller");

        await _refundRepository.AddAsync(refund, cancellationToken);
        await _refundRepository.SaveChangesAsync(cancellationToken);

        // Process the refund with payment provider
        var processResult = await ProcessRefundWithProviderAsync(refund, order, cancellationToken);

        return processResult.IsSuccess
            ? InitiateRefundResultDto.Succeeded(refund.Id, processResult.RefundTransactionId, processResult.Status)
            : InitiateRefundResultDto.Failed(processResult.ErrorMessage ?? "Failed to process refund.");
    }

    /// <summary>
    /// Validates if a seller can initiate a refund based on business rules.
    /// </summary>
    public async Task<SellerRefundValidationDto> HandleAsync(
        ValidateSellerRefundQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Get shipment and order
        var (shipment, order, _) = await _orderRepository.GetShipmentWithOrderAsync(
            query.ShipmentId, cancellationToken);

        if (shipment is null || order is null)
        {
            return new SellerRefundValidationDto(false, "Shipment not found.", 0m, false);
        }

        // Verify shipment belongs to the store
        if (shipment.StoreId != query.StoreId)
        {
            return new SellerRefundValidationDto(false, "Shipment does not belong to this store.", 0m, false);
        }

        // Check order status allows refund
        if (order.Status == OrderStatus.Pending || order.Status == OrderStatus.PaymentFailed)
        {
            return new SellerRefundValidationDto(false, "Order has not been paid yet.", 0m, false);
        }

        if (order.Status == OrderStatus.Refunded)
        {
            return new SellerRefundValidationDto(false, "Order has already been refunded.", 0m, false);
        }

        // Check shipment status - cannot refund already refunded shipment
        if (shipment.Status == ShipmentStatus.Refunded)
        {
            return new SellerRefundValidationDto(false, "Shipment has already been refunded.", 0m, false);
        }

        // Business rule: Check refund window
        var daysSinceOrder = (DateTime.UtcNow - order.CreatedAt).TotalDays;
        if (daysSinceOrder > SellerRefundWindowDays)
        {
            return new SellerRefundValidationDto(
                false, 
                $"Refund window of {SellerRefundWindowDays} days has expired. Contact support.", 
                0m, 
                false);
        }

        // Calculate max refundable amount
        var shipmentTotal = shipment.Subtotal + shipment.ShippingCost;
        
        // Check existing refunds for this shipment
        var existingRefunds = await _refundRepository.GetByShipmentIdAsync(query.ShipmentId, cancellationToken);
        var alreadyRefunded = existingRefunds
            .Where(r => r.Status == RefundStatus.Completed)
            .Sum(r => r.Amount);

        var maxRefundable = shipmentTotal - alreadyRefunded;
        if (maxRefundable <= 0)
        {
            return new SellerRefundValidationDto(false, "Shipment has already been fully refunded.", 0m, false);
        }

        // Validate requested amount
        if (query.RequestedAmount.HasValue)
        {
            if (query.RequestedAmount.Value <= 0)
            {
                return new SellerRefundValidationDto(false, "Refund amount must be greater than zero.", maxRefundable, false);
            }

            if (query.RequestedAmount.Value > maxRefundable)
            {
                return new SellerRefundValidationDto(
                    false, 
                    $"Requested amount exceeds maximum refundable amount of {maxRefundable}.", 
                    maxRefundable, 
                    false);
            }
        }

        // Check escrow status - ensure funds are still held
        var allocation = await _escrowRepository.GetAllocationByShipmentIdAsync(query.ShipmentId, cancellationToken);
        if (allocation is null)
        {
            // No escrow - might be legacy order, allow refund but flag for attention
            return new SellerRefundValidationDto(true, "No escrow found. Refund will proceed.", maxRefundable, true);
        }

        if (allocation.Status == EscrowAllocationStatus.Released)
        {
            return new SellerRefundValidationDto(
                false, 
                "Funds have already been released to seller. Contact support for refund.", 
                0m, 
                false);
        }

        return new SellerRefundValidationDto(true, null, maxRefundable, false);
    }

    /// <summary>
    /// Retries a failed refund.
    /// </summary>
    public async Task<ProcessRefundResultDto> HandleAsync(
        RetryRefundCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var refund = await _refundRepository.GetByIdAsync(command.RefundId, cancellationToken);
        if (refund is null)
        {
            return ProcessRefundResultDto.Failed("Refund not found.", RefundStatus.Failed.ToString());
        }

        if (!refund.CanRetry())
        {
            return ProcessRefundResultDto.Failed(
                "Refund cannot be retried. Max retries exceeded or wrong status.",
                refund.Status.ToString());
        }

        var order = await _orderRepository.GetByIdAsync(refund.OrderId, cancellationToken);
        if (order is null)
        {
            return ProcessRefundResultDto.Failed("Order not found.", RefundStatus.Failed.ToString());
        }

        refund.ResetForRetry();
        await _refundRepository.UpdateAsync(refund, cancellationToken);
        await _refundRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Retrying refund {RefundId} (attempt {RetryCount}) initiated by {InitiatedById}",
            command.RefundId, refund.RetryCount + 1, command.InitiatedById);

        return await ProcessRefundWithProviderAsync(refund, order, cancellationToken);
    }

    /// <summary>
    /// Gets a refund by ID.
    /// </summary>
    public async Task<RefundDto?> HandleAsync(
        GetRefundByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var refund = await _refundRepository.GetByIdAsync(query.RefundId, cancellationToken);
        return refund is null ? null : MapToDto(refund);
    }

    /// <summary>
    /// Gets all refunds for an order.
    /// </summary>
    public async Task<IReadOnlyList<RefundDto>> HandleAsync(
        GetOrderRefundsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var refunds = await _refundRepository.GetByOrderIdAsync(query.OrderId, cancellationToken);
        return refunds.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Gets refund summary for an order.
    /// </summary>
    public async Task<OrderRefundSummaryDto?> HandleAsync(
        GetOrderRefundSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var order = await _orderRepository.GetByIdAsync(query.OrderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        var refunds = await _refundRepository.GetByOrderIdAsync(query.OrderId, cancellationToken);
        var totalRefunded = refunds
            .Where(r => r.Status == RefundStatus.Completed)
            .Sum(r => r.Amount);

        return new OrderRefundSummaryDto(
            query.OrderId,
            order.TotalAmount,
            totalRefunded,
            order.TotalAmount - totalRefunded,
            refunds.Count,
            refunds.Select(MapToDto).ToList(),
            order.Currency);
    }

    /// <summary>
    /// Gets refunds for a store with optional status filter.
    /// </summary>
    public async Task<PagedResultDto<RefundDto>> HandleAsync(
        GetStoreRefundsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        RefundStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<RefundStatus>(query.Status, ignoreCase: true, out var parsedStatus))
        {
            status = parsedStatus;
        }

        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var skip = (pageNumber - 1) * pageSize;

        var (refunds, totalCount) = await _refundRepository.GetByStoreIdAsync(
            query.StoreId, status, skip, pageSize, cancellationToken);

        return PagedResultDto<RefundDto>.Create(
            refunds.Select(MapToDto).ToList(),
            pageNumber,
            pageSize,
            totalCount);
    }

    /// <summary>
    /// Processes a refund with the payment provider.
    /// </summary>
    private async Task<ProcessRefundResultDto> ProcessRefundWithProviderAsync(
        Refund refund,
        Order order,
        CancellationToken cancellationToken)
    {
        refund.StartProcessing();
        await _refundRepository.UpdateAsync(refund, cancellationToken);
        await _refundRepository.SaveChangesAsync(cancellationToken);

        try
        {
            // Call payment provider
            RefundProviderResult providerResult;

            if (refund.Type == RefundType.Full)
            {
                providerResult = await _refundProviderService.ProcessFullRefundAsync(
                    refund.OrderId,
                    refund.OriginalTransactionId ?? "",
                    refund.Amount,
                    refund.Currency,
                    refund.IdempotencyKey,
                    refund.Reason,
                    cancellationToken);
            }
            else
            {
                providerResult = await _refundProviderService.ProcessPartialRefundAsync(
                    refund.OrderId,
                    refund.OriginalTransactionId ?? "",
                    refund.Amount,
                    refund.Currency,
                    refund.IdempotencyKey,
                    refund.Reason,
                    cancellationToken);
            }

            if (providerResult.IsSuccess)
            {
                if (providerResult.Status == RefundProviderStatus.Completed)
                {
                    refund.Complete(providerResult.RefundTransactionId ?? $"REF-{Guid.NewGuid():N}");
                    
                    // Update escrow and order
                    await UpdateEscrowAndOrderAfterRefundAsync(refund, order, cancellationToken);
                    
                    // Send notification to buyer
                    await SendRefundNotificationAsync(refund, order, cancellationToken);

                    _logger.LogInformation(
                        "Refund {RefundId} completed successfully. Transaction: {TransactionId}",
                        refund.Id, refund.RefundTransactionId);

                    return ProcessRefundResultDto.Succeeded(
                        refund.RefundTransactionId!,
                        refund.Status.ToString(),
                        refund.Amount,
                        refund.CommissionRefundAmount);
                }
                else
                {
                    // Pending status - provider is processing async
                    _logger.LogInformation(
                        "Refund {RefundId} is pending with provider. Will check status later.",
                        refund.Id);

                    return ProcessRefundResultDto.Pending(RefundStatus.Processing.ToString());
                }
            }
            else
            {
                refund.Fail(providerResult.ErrorMessage, providerResult.ErrorCode);
                
                _logger.LogWarning(
                    "Refund {RefundId} failed. Error: {ErrorMessage} ({ErrorCode})",
                    refund.Id, providerResult.ErrorMessage, providerResult.ErrorCode);

                // Send error notification to support agent
                await SendRefundErrorNotificationAsync(refund, providerResult.ErrorMessage, cancellationToken);

                return ProcessRefundResultDto.Failed(
                    providerResult.ErrorMessage ?? "Payment provider error",
                    refund.Status.ToString());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while processing refund {RefundId}", refund.Id);
            
            refund.Fail(ex.Message, "EXCEPTION");
            
            await SendRefundErrorNotificationAsync(refund, ex.Message, cancellationToken);

            return ProcessRefundResultDto.Failed(ex.Message, refund.Status.ToString());
        }
        finally
        {
            await _refundRepository.UpdateAsync(refund, cancellationToken);
            await _refundRepository.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Updates escrow allocations and order status after a successful refund.
    /// </summary>
    private async Task UpdateEscrowAndOrderAfterRefundAsync(
        Refund refund,
        Order order,
        CancellationToken cancellationToken)
    {
        if (refund.ShipmentId.HasValue)
        {
            // Shipment-level refund
            if (refund.Type == RefundType.Full)
            {
                await _escrowService.HandleAsync(
                    new RefundShipmentEscrowCommand(refund.ShipmentId.Value, refund.RefundTransactionId),
                    cancellationToken);
            }
            else
            {
                await _escrowService.HandleAsync(
                    new PartialRefundEscrowCommand(refund.ShipmentId.Value, refund.Amount, refund.RefundTransactionId),
                    cancellationToken);
            }
        }
        else
        {
            // Order-level refund
            if (refund.Type == RefundType.Full)
            {
                await _escrowService.HandleAsync(
                    new RefundOrderEscrowCommand(order.Id, refund.RefundTransactionId),
                    cancellationToken);
                
                // Mark order as refunded
                order.Refund(refund.Amount);
            }
            else
            {
                // For partial order refunds, apply proportionally across shipments
                var escrow = await _escrowRepository.GetByOrderIdAsync(order.Id, cancellationToken);
                if (escrow is not null)
                {
                    var heldAllocations = escrow.Allocations
                        .Where(a => a.Status == EscrowAllocationStatus.Held)
                        .ToList();

                    if (heldAllocations.Count > 0)
                    {
                        var totalHeld = heldAllocations.Sum(a => a.TotalAmount);
                        var remainingRefund = refund.Amount;

                        foreach (var allocation in heldAllocations)
                        {
                            if (remainingRefund <= 0) break;

                            var proportionalRefund = Math.Min(
                                Math.Round(refund.Amount * (allocation.TotalAmount / totalHeld), 2),
                                allocation.GetRemainingAmount());

                            if (proportionalRefund > 0)
                            {
                                await _escrowService.HandleAsync(
                                    new PartialRefundEscrowCommand(
                                        allocation.ShipmentId,
                                        proportionalRefund,
                                        refund.RefundTransactionId),
                                    cancellationToken);

                                remainingRefund -= proportionalRefund;
                            }
                        }
                    }
                }
            }
        }

        await _orderRepository.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Sends notification to buyer about the refund.
    /// </summary>
    private async Task SendRefundNotificationAsync(
        Refund refund,
        Order order,
        CancellationToken cancellationToken)
    {
        var buyer = await _userRepository.GetByIdAsync(order.BuyerId, cancellationToken);
        if (buyer?.Email is not null)
        {
            await _notificationService.SendRefundProcessedAsync(
                order.Id,
                buyer.Email.Value,
                order.OrderNumber,
                refund.Amount,
                refund.Currency,
                cancellationToken);
        }
    }

    /// <summary>
    /// Sends error notification for failed refunds.
    /// </summary>
    private async Task SendRefundErrorNotificationAsync(
        Refund refund,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Refund {RefundId} failed with error: {ErrorMessage}. " +
            "Initiator: {InitiatorType} {InitiatedById}. Can retry: {CanRetry}",
            refund.Id,
            errorMessage,
            refund.InitiatorType,
            refund.InitiatedById,
            refund.CanRetry());

        // In a production system, this would notify the support agent or seller
        // who initiated the refund about the failure
    }

    private static RefundDto MapToDto(Refund refund)
    {
        return new RefundDto(
            refund.Id,
            refund.OrderId,
            refund.ShipmentId,
            refund.BuyerId,
            refund.StoreId,
            refund.Type.ToString(),
            refund.Status.ToString(),
            refund.Amount,
            refund.Currency,
            refund.CommissionRefundAmount,
            refund.Reason,
            refund.RefundTransactionId,
            refund.InitiatedById,
            refund.InitiatorType,
            refund.ErrorMessage,
            refund.ErrorCode,
            refund.RetryCount,
            refund.CanRetry(),
            refund.CreatedAt,
            refund.CompletedAt);
    }
}
