using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Domain.Services;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for managing escrow payments.
/// Handles creation, release, and refund of escrowed funds.
/// </summary>
public sealed class EscrowService
{
    private readonly IEscrowRepository _escrowRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly INotificationService _notificationService;
    private readonly CommissionCalculator _commissionCalculator;

    public EscrowService(
        IEscrowRepository escrowRepository,
        IOrderRepository orderRepository,
        INotificationService notificationService,
        CommissionCalculator commissionCalculator)
    {
        _escrowRepository = escrowRepository;
        _orderRepository = orderRepository;
        _notificationService = notificationService;
        _commissionCalculator = commissionCalculator;
    }

    /// <summary>
    /// Creates an escrow payment for an order after successful payment confirmation.
    /// Splits the payment into per-seller allocations with commission deduction.
    /// </summary>
    public async Task<CreateEscrowResultDto> CreateEscrowForOrderAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        // Check if escrow already exists for this order
        var existingEscrow = await _escrowRepository.GetByOrderIdAsync(order.Id, cancellationToken);
        if (existingEscrow is not null)
        {
            return CreateEscrowResultDto.Succeeded(existingEscrow.Id);
        }

        // Create the escrow payment
        var escrow = new EscrowPayment(
            order.Id,
            order.BuyerId,
            order.TotalAmount,
            order.Currency,
            order.PaymentTransactionId);

        // Create allocations for each shipment (seller)
        foreach (var shipment in order.Shipments)
        {
            // Calculate commission for the seller
            var commission = _commissionCalculator.CalculateCommission(
                shipment.StoreId,
                shipment.Subtotal,
                order.Currency);

            escrow.AddAllocation(
                shipment.StoreId,
                shipment.Id,
                shipment.Subtotal,
                shipment.ShippingCost,
                commission.CommissionAmount.Amount,
                commission.CommissionRate);
        }

        await _escrowRepository.AddAsync(escrow, cancellationToken);

        // Create audit ledger entry for escrow creation
        var createdEntry = EscrowLedger.CreateCreatedEntry(escrow);
        await _escrowRepository.AddLedgerEntryAsync(createdEntry, cancellationToken);

        // Create audit ledger entries for each allocation
        foreach (var allocation in escrow.Allocations)
        {
            var allocationEntry = EscrowLedger.CreateAllocationEntry(escrow, allocation);
            await _escrowRepository.AddLedgerEntryAsync(allocationEntry, cancellationToken);
        }

        await _escrowRepository.SaveChangesAsync(cancellationToken);

        return CreateEscrowResultDto.Succeeded(escrow.Id);
    }

    /// <summary>
    /// Gets escrow payment by order ID.
    /// </summary>
    public async Task<EscrowPaymentDto?> HandleAsync(
        GetEscrowByOrderIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var escrow = await _escrowRepository.GetByOrderIdAsync(query.OrderId, cancellationToken);
        return escrow is null ? null : MapToDto(escrow);
    }

    /// <summary>
    /// Gets escrow payment by ID.
    /// </summary>
    public async Task<EscrowPaymentDto?> HandleAsync(
        GetEscrowByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var escrow = await _escrowRepository.GetByIdAsync(query.EscrowPaymentId, cancellationToken);
        return escrow is null ? null : MapToDto(escrow);
    }

    /// <summary>
    /// Gets seller's escrow balance summary.
    /// </summary>
    public async Task<SellerEscrowBalanceDto> HandleAsync(
        GetSellerEscrowBalanceQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var heldAllocations = await _escrowRepository.GetHeldAllocationsByStoreIdAsync(
            query.StoreId, cancellationToken);

        var totalHeld = heldAllocations.Sum(a => a.SellerPayout);
        var totalCommissions = heldAllocations.Sum(a => a.CommissionAmount);
        var eligibleAllocations = heldAllocations.Where(a => a.IsEligibleForPayout).ToList();
        var totalEligible = eligibleAllocations.Sum(a => a.SellerPayout);

        // Get currency from the first allocation, default to USD if no allocations exist
        var currency = heldAllocations.FirstOrDefault()?.Currency ?? "USD";

        return new SellerEscrowBalanceDto(
            query.StoreId,
            totalHeld,
            totalEligible,
            totalCommissions,
            heldAllocations.Count,
            eligibleAllocations.Count,
            currency);
    }

    /// <summary>
    /// Gets all held escrow allocations for a seller.
    /// </summary>
    public async Task<IReadOnlyList<EscrowAllocationDto>> HandleAsync(
        GetHeldEscrowAllocationsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var allocations = await _escrowRepository.GetHeldAllocationsByStoreIdAsync(
            query.StoreId, cancellationToken);

        return allocations.Select(MapAllocationToDto).ToList();
    }

    /// <summary>
    /// Gets escrow allocations eligible for payout for a seller.
    /// </summary>
    public async Task<IReadOnlyList<EscrowAllocationDto>> HandleAsync(
        GetEligiblePayoutsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var allocations = await _escrowRepository.GetEligibleForPayoutByStoreIdAsync(
            query.StoreId, cancellationToken);

        return allocations.Select(MapAllocationToDto).ToList();
    }

    /// <summary>
    /// Gets released escrow allocations for a seller with pagination.
    /// </summary>
    public async Task<PagedResultDto<EscrowAllocationDto>> HandleAsync(
        GetReleasedEscrowAllocationsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (allocations, totalCount) = await _escrowRepository.GetReleasedAllocationsByStoreIdAsync(
            query.StoreId,
            query.Skip,
            query.Take,
            cancellationToken);

        var dtos = allocations.Select(MapAllocationToDto).ToList();

        var pageSize = query.Take > 0 ? query.Take : 20;
        var pageNumber = (query.Skip / pageSize) + 1;

        return PagedResultDto<EscrowAllocationDto>.Create(dtos, pageNumber, pageSize, totalCount);
    }

    /// <summary>
    /// Marks an escrow allocation as eligible for payout.
    /// Called when conditions for payout are met (e.g., delivery confirmed).
    /// </summary>
    public async Task<bool> HandleAsync(
        MarkEscrowEligibleCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var allocation = await _escrowRepository.GetAllocationByShipmentIdAsync(
            command.ShipmentId, cancellationToken);

        if (allocation is null)
        {
            return false;
        }

        if (allocation.Status != EscrowAllocationStatus.Held)
        {
            return false;
        }

        allocation.MarkEligibleForPayout();

        // Get the escrow payment to update it
        var escrow = await _escrowRepository.GetByIdAsync(allocation.EscrowPaymentId, cancellationToken);
        if (escrow is not null)
        {
            // Create audit ledger entry for eligibility
            var eligibleEntry = EscrowLedger.CreateEligibleEntry(escrow, allocation);
            await _escrowRepository.AddLedgerEntryAsync(eligibleEntry, cancellationToken);

            await _escrowRepository.UpdateAsync(escrow, cancellationToken);
            await _escrowRepository.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    /// <summary>
    /// Releases escrow funds to a seller for a specific shipment.
    /// </summary>
    public async Task<ReleaseEscrowResultDto> HandleAsync(
        ReleaseEscrowCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var allocation = await _escrowRepository.GetAllocationByShipmentIdAsync(
            command.ShipmentId, cancellationToken);

        if (allocation is null)
        {
            return ReleaseEscrowResultDto.Failed("Escrow allocation not found for this shipment.");
        }

        if (allocation.StoreId != command.StoreId)
        {
            return ReleaseEscrowResultDto.Failed("Escrow allocation does not belong to this store.");
        }

        if (!allocation.CanBeReleased())
        {
            return ReleaseEscrowResultDto.Failed("Escrow allocation is not eligible for release.");
        }

        var escrow = await _escrowRepository.GetByIdAsync(allocation.EscrowPaymentId, cancellationToken);
        if (escrow is null)
        {
            return ReleaseEscrowResultDto.Failed("Escrow payment not found.");
        }

        var releasedAmount = allocation.SellerPayout;
        escrow.ReleaseAllocation(command.ShipmentId, command.PayoutReference);

        // Create audit ledger entry for release
        var releaseEntry = EscrowLedger.CreateReleaseEntry(escrow, allocation, command.PayoutReference);
        await _escrowRepository.AddLedgerEntryAsync(releaseEntry, cancellationToken);

        await _escrowRepository.UpdateAsync(escrow, cancellationToken);
        await _escrowRepository.SaveChangesAsync(cancellationToken);

        return ReleaseEscrowResultDto.Succeeded(releasedAmount, command.PayoutReference);
    }

    /// <summary>
    /// Refunds escrow allocation back to buyer for a specific shipment.
    /// Called when a shipment is cancelled.
    /// </summary>
    public async Task<RefundEscrowResultDto> HandleAsync(
        RefundShipmentEscrowCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var allocation = await _escrowRepository.GetAllocationByShipmentIdAsync(
            command.ShipmentId, cancellationToken);

        if (allocation is null)
        {
            // No escrow allocation exists for this shipment, which is fine - 
            // the order might have been created before escrow was implemented
            return RefundEscrowResultDto.Succeeded(0m, null);
        }

        // If already refunded, return success with the refunded amount (idempotent)
        if (allocation.Status == EscrowAllocationStatus.Refunded)
        {
            return RefundEscrowResultDto.Succeeded(allocation.TotalAmount, allocation.RefundReference);
        }

        if (!allocation.CanBeRefunded())
        {
            return RefundEscrowResultDto.Failed("Escrow allocation cannot be refunded.");
        }

        var escrow = await _escrowRepository.GetByIdAsync(allocation.EscrowPaymentId, cancellationToken);
        if (escrow is null)
        {
            return RefundEscrowResultDto.Failed("Escrow payment not found.");
        }

        var refundedAmount = allocation.TotalAmount;
        escrow.RefundAllocation(command.ShipmentId, command.RefundReference);

        // Create audit ledger entry for refund
        var refundEntry = EscrowLedger.CreateRefundEntry(escrow, allocation, command.RefundReference);
        await _escrowRepository.AddLedgerEntryAsync(refundEntry, cancellationToken);

        await _escrowRepository.UpdateAsync(escrow, cancellationToken);
        await _escrowRepository.SaveChangesAsync(cancellationToken);

        return RefundEscrowResultDto.Succeeded(refundedAmount, command.RefundReference);
    }

    /// <summary>
    /// Refunds full escrow back to buyer.
    /// Called when an order is cancelled.
    /// </summary>
    public async Task<RefundEscrowResultDto> HandleAsync(
        RefundOrderEscrowCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var escrow = await _escrowRepository.GetByOrderIdAsync(command.OrderId, cancellationToken);
        if (escrow is null)
        {
            return RefundEscrowResultDto.Failed("Escrow payment not found for this order.");
        }

        if (escrow.Status == EscrowStatus.Refunded)
        {
            return RefundEscrowResultDto.Succeeded(escrow.RefundedAmount, null);
        }

        if (escrow.Status == EscrowStatus.Released)
        {
            return RefundEscrowResultDto.Failed("Cannot refund fully released escrow.");
        }

        var refundableAllocations = escrow.Allocations
            .Where(a => a.Status == EscrowAllocationStatus.Held)
            .ToList();

        var refundableAmount = refundableAllocations.Sum(a => a.TotalAmount);

        escrow.RefundFull(command.RefundReference);

        // Create audit ledger entries for each refunded allocation
        foreach (var allocation in refundableAllocations)
        {
            var refundEntry = EscrowLedger.CreateRefundEntry(escrow, allocation, command.RefundReference);
            await _escrowRepository.AddLedgerEntryAsync(refundEntry, cancellationToken);
        }

        await _escrowRepository.UpdateAsync(escrow, cancellationToken);
        await _escrowRepository.SaveChangesAsync(cancellationToken);

        return RefundEscrowResultDto.Succeeded(refundableAmount, command.RefundReference);
    }

    private static EscrowPaymentDto MapToDto(EscrowPayment escrow)
    {
        return new EscrowPaymentDto(
            escrow.Id,
            escrow.OrderId,
            escrow.BuyerId,
            escrow.TotalAmount,
            escrow.Currency,
            escrow.Status.ToString(),
            escrow.ReleasedAmount,
            escrow.RefundedAmount,
            escrow.Allocations.Select(MapAllocationToDto).ToList(),
            escrow.CreatedAt,
            escrow.ReleasedAt,
            escrow.RefundedAt);
    }

    private static EscrowAllocationDto MapAllocationToDto(EscrowAllocation allocation)
    {
        return new EscrowAllocationDto(
            allocation.Id,
            allocation.StoreId,
            allocation.ShipmentId,
            allocation.Currency,
            allocation.SellerAmount,
            allocation.ShippingAmount,
            allocation.TotalAmount,
            allocation.CommissionAmount,
            allocation.CommissionRate,
            allocation.SellerPayout,
            allocation.Status.ToString(),
            allocation.IsEligibleForPayout,
            allocation.CreatedAt,
            allocation.ReleasedAt,
            allocation.RefundedAt,
            allocation.PayoutEligibleAt);
    }
}
