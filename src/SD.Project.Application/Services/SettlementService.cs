using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for managing monthly seller settlements.
/// Handles generation, finalization, approval, and export of settlements.
/// </summary>
public sealed class SettlementService
{
    private readonly ISettlementRepository _settlementRepository;
    private readonly IEscrowRepository _escrowRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly INotificationService _notificationService;

    /// <summary>
    /// Default currency when none is available.
    /// </summary>
    public const string DefaultCurrency = "EUR";

    public SettlementService(
        ISettlementRepository settlementRepository,
        IEscrowRepository escrowRepository,
        IStoreRepository storeRepository,
        IOrderRepository orderRepository,
        INotificationService notificationService)
    {
        _settlementRepository = settlementRepository;
        _escrowRepository = escrowRepository;
        _storeRepository = storeRepository;
        _orderRepository = orderRepository;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Generates a monthly settlement for a specific store.
    /// </summary>
    public async Task<GenerateSettlementResultDto> HandleAsync(
        GenerateSettlementCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Validate store exists
        var store = await _storeRepository.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null)
        {
            return GenerateSettlementResultDto.Failed("Store not found.");
        }

        // Check for existing settlement
        var existingSettlement = await _settlementRepository.GetLatestByStoreAndPeriodAsync(
            command.StoreId, command.Year, command.Month, cancellationToken);

        if (existingSettlement is not null && !command.Regenerate)
        {
            return GenerateSettlementResultDto.Failed(
                $"Settlement already exists for {command.Year}-{command.Month:D2}. Use regenerate to create a new version.");
        }

        // Determine version number
        int version = 1;
        if (command.Regenerate && existingSettlement is not null)
        {
            version = await _settlementRepository.GetNextVersionAsync(
                command.StoreId, command.Year, command.Month, cancellationToken);
        }

        // Get all escrow allocations for the store in the period
        var allocations = await _escrowRepository.GetAllocationsByStoreIdAsync(
            command.StoreId, cancellationToken);

        // Calculate period boundaries
        var periodStart = new DateTime(command.Year, command.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1).AddTicks(-1);

        // Filter allocations to the period
        var periodAllocations = allocations
            .Where(a => a.CreatedAt >= periodStart && a.CreatedAt <= periodEnd)
            .ToList();

        if (periodAllocations.Count == 0)
        {
            return GenerateSettlementResultDto.NoData();
        }

        // Determine currency from allocations
        var currency = periodAllocations.FirstOrDefault()?.Currency ?? DefaultCurrency;

        // Create settlement
        var settlement = new Settlement(
            command.StoreId,
            store.SellerId,
            command.Year,
            command.Month,
            currency,
            version);

        // Add items from escrow allocations
        foreach (var allocation in periodAllocations)
        {
            // Get order number for the shipment
            var (_, order, _) = await _orderRepository.GetShipmentWithOrderAsync(
                allocation.ShipmentId, cancellationToken);

            settlement.AddItem(
                allocation.Id,
                allocation.ShipmentId,
                order?.OrderNumber,
                allocation.SellerAmount,
                allocation.ShippingAmount,
                allocation.CommissionAmount,
                allocation.RefundedAmount,
                allocation.CreatedAt);
        }

        await _settlementRepository.AddAsync(settlement, cancellationToken);
        await _settlementRepository.SaveChangesAsync(cancellationToken);

        // Notify about settlement generation
        await _notificationService.SendSettlementGeneratedNotificationAsync(
            store.SellerId,
            settlement.Id,
            settlement.SettlementNumber,
            settlement.NetPayable,
            currency,
            command.Year,
            command.Month,
            cancellationToken);

        return GenerateSettlementResultDto.Succeeded(
            settlement.Id,
            settlement.SettlementNumber,
            settlement.NetPayable,
            settlement.Items.Count);
    }

    /// <summary>
    /// Generates settlements for all stores with activity in the period.
    /// </summary>
    public async Task<IReadOnlyList<GenerateSettlementResultDto>> HandleAsync(
        GenerateAllSettlementsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var results = new List<GenerateSettlementResultDto>();

        // Get all stores with escrow activity but no settlement
        var storeIds = await _settlementRepository.GetStoresWithoutSettlementAsync(
            command.Year, command.Month, cancellationToken);

        // If regenerating, also include stores with existing settlements
        if (command.Regenerate)
        {
            var existingSettlements = await _settlementRepository.GetByPeriodAsync(
                command.Year, command.Month, cancellationToken);
            var storeIdsWithSettlements = existingSettlements.Select(s => s.StoreId);
            storeIds = storeIds.Union(storeIdsWithSettlements).Distinct().ToList();
        }

        foreach (var storeId in storeIds)
        {
            var result = await HandleAsync(
                new GenerateSettlementCommand(storeId, command.Year, command.Month, command.Regenerate),
                cancellationToken);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// Finalizes a settlement.
    /// </summary>
    public async Task<FinalizeSettlementResultDto> HandleAsync(
        FinalizeSettlementCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var settlement = await _settlementRepository.GetByIdAsync(command.SettlementId, cancellationToken);
        if (settlement is null)
        {
            return FinalizeSettlementResultDto.Failed("Settlement not found.");
        }

        try
        {
            settlement.FinalizeSettlement();
            await _settlementRepository.UpdateAsync(settlement, cancellationToken);
            await _settlementRepository.SaveChangesAsync(cancellationToken);

            return FinalizeSettlementResultDto.Succeeded(settlement.Id);
        }
        catch (InvalidOperationException ex)
        {
            return FinalizeSettlementResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Approves a settlement.
    /// </summary>
    public async Task<ApproveSettlementResultDto> HandleAsync(
        ApproveSettlementCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var settlement = await _settlementRepository.GetByIdAsync(command.SettlementId, cancellationToken);
        if (settlement is null)
        {
            return ApproveSettlementResultDto.Failed("Settlement not found.");
        }

        try
        {
            settlement.Approve(command.ApprovedBy);
            await _settlementRepository.UpdateAsync(settlement, cancellationToken);
            await _settlementRepository.SaveChangesAsync(cancellationToken);

            return ApproveSettlementResultDto.Succeeded(settlement.Id);
        }
        catch (InvalidOperationException ex)
        {
            return ApproveSettlementResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Adds an adjustment to a settlement.
    /// </summary>
    public async Task<GenerateSettlementResultDto> HandleAsync(
        AddSettlementAdjustmentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var settlement = await _settlementRepository.GetByIdAsync(command.SettlementId, cancellationToken);
        if (settlement is null)
        {
            return GenerateSettlementResultDto.Failed("Settlement not found.");
        }

        try
        {
            settlement.AddAdjustment(
                command.OriginalYear,
                command.OriginalMonth,
                command.Amount,
                command.Reason,
                command.RelatedOrderId,
                command.RelatedOrderNumber);

            await _settlementRepository.UpdateAsync(settlement, cancellationToken);
            await _settlementRepository.SaveChangesAsync(cancellationToken);

            return GenerateSettlementResultDto.Succeeded(
                settlement.Id,
                settlement.SettlementNumber,
                settlement.NetPayable,
                settlement.Items.Count);
        }
        catch (InvalidOperationException ex)
        {
            return GenerateSettlementResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Updates settlement notes.
    /// </summary>
    public async Task<bool> HandleAsync(
        UpdateSettlementNotesCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var settlement = await _settlementRepository.GetByIdAsync(command.SettlementId, cancellationToken);
        if (settlement is null)
        {
            return false;
        }

        settlement.UpdateNotes(command.Notes);
        await _settlementRepository.UpdateAsync(settlement, cancellationToken);
        await _settlementRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Marks a settlement as exported.
    /// </summary>
    public async Task<bool> HandleAsync(
        ExportSettlementCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var settlement = await _settlementRepository.GetByIdAsync(command.SettlementId, cancellationToken);
        if (settlement is null)
        {
            return false;
        }

        try
        {
            settlement.MarkExported();
            await _settlementRepository.UpdateAsync(settlement, cancellationToken);
            await _settlementRepository.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets a settlement by ID.
    /// </summary>
    public async Task<SettlementDto?> HandleAsync(
        GetSettlementByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var settlement = await _settlementRepository.GetByIdAsync(query.SettlementId, cancellationToken);
        if (settlement is null)
        {
            return null;
        }

        var store = await _storeRepository.GetByIdAsync(settlement.StoreId, cancellationToken);
        return MapToDto(settlement, store?.Name ?? "Unknown Store");
    }

    /// <summary>
    /// Gets settlement details with items and adjustments.
    /// </summary>
    public async Task<SettlementDetailsDto?> HandleAsync(
        GetSettlementDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var settlement = await _settlementRepository.GetByIdAsync(query.SettlementId, cancellationToken);
        if (settlement is null)
        {
            return null;
        }

        var store = await _storeRepository.GetByIdAsync(settlement.StoreId, cancellationToken);
        return MapToDetailsDto(settlement, store?.Name ?? "Unknown Store");
    }

    /// <summary>
    /// Gets settlements for a store with pagination.
    /// </summary>
    public async Task<PagedResultDto<SettlementListItemDto>> HandleAsync(
        GetSettlementsByStoreIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (settlements, totalCount) = await _settlementRepository.GetByStoreIdAsync(
            query.StoreId, query.Skip, query.Take, cancellationToken);

        var store = await _storeRepository.GetByIdAsync(query.StoreId, cancellationToken);
        var storeName = store?.Name ?? "Unknown Store";

        var dtos = settlements.Select(s => MapToListItemDto(s, storeName)).ToList();

        var pageSize = query.Take > 0 ? query.Take : 20;
        var pageNumber = (query.Skip / pageSize) + 1;

        return PagedResultDto<SettlementListItemDto>.Create(dtos, pageNumber, pageSize, totalCount);
    }

    /// <summary>
    /// Gets settlements for admin view with filtering and pagination.
    /// </summary>
    public async Task<PagedResultDto<SettlementListItemDto>> HandleAsync(
        GetSettlementsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var skip = (query.PageNumber - 1) * query.PageSize;
        var (settlements, totalCount) = await _settlementRepository.GetFilteredAsync(
            query.StoreId,
            query.Year,
            query.Month,
            query.Status,
            skip,
            query.PageSize,
            cancellationToken);

        // Get store names
        var storeIds = settlements.Select(s => s.StoreId).Distinct().ToList();
        var stores = await _storeRepository.GetByIdsAsync(storeIds, cancellationToken);
        var storeNames = stores.ToDictionary(s => s.Id, s => s.Name);

        var dtos = settlements.Select(s => 
            MapToListItemDto(s, storeNames.GetValueOrDefault(s.StoreId, "Unknown Store"))).ToList();

        return PagedResultDto<SettlementListItemDto>.Create(dtos, query.PageNumber, query.PageSize, totalCount);
    }

    /// <summary>
    /// Gets settlements summary for a period.
    /// </summary>
    public async Task<SettlementsSummaryDto> HandleAsync(
        GetSettlementsSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (settlements, _) = await _settlementRepository.GetFilteredAsync(
            null, query.Year, query.Month, null, 0, int.MaxValue, cancellationToken);

        var currency = settlements.FirstOrDefault()?.Currency ?? DefaultCurrency;

        return new SettlementsSummaryDto(
            settlements.Count,
            settlements.Count(s => s.Status == SettlementStatus.Draft),
            settlements.Count(s => s.Status == SettlementStatus.Finalized),
            settlements.Count(s => s.Status == SettlementStatus.Approved),
            settlements.Count(s => s.Status == SettlementStatus.Exported),
            settlements.Sum(s => s.NetPayable),
            currency);
    }

    /// <summary>
    /// Gets settlement export data.
    /// </summary>
    public async Task<SettlementExportDto?> HandleAsync(
        GetSettlementExportQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var settlement = await _settlementRepository.GetByIdAsync(query.SettlementId, cancellationToken);
        if (settlement is null)
        {
            return null;
        }

        var store = await _storeRepository.GetByIdAsync(settlement.StoreId, cancellationToken);

        return new SettlementExportDto(
            settlement.SettlementNumber,
            store?.Name ?? "Unknown Store",
            $"{settlement.Year}-{settlement.Month:D2}",
            settlement.Currency,
            settlement.GrossSales,
            settlement.TotalShipping,
            settlement.TotalCommission,
            settlement.TotalRefunds,
            settlement.TotalAdjustments,
            settlement.NetPayable,
            settlement.OrderCount,
            settlement.Status.ToString(),
            settlement.CreatedAt);
    }

    /// <summary>
    /// Gets settlement items export data.
    /// </summary>
    public async Task<IReadOnlyList<SettlementItemExportDto>> HandleAsync(
        GetSettlementItemsExportQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var settlement = await _settlementRepository.GetByIdAsync(query.SettlementId, cancellationToken);
        if (settlement is null)
        {
            return Array.Empty<SettlementItemExportDto>();
        }

        return settlement.Items.Select(i => new SettlementItemExportDto(
            settlement.SettlementNumber,
            i.OrderNumber ?? "N/A",
            i.SellerAmount,
            i.ShippingAmount,
            i.CommissionAmount,
            i.RefundedAmount,
            i.NetAmount,
            i.TransactionDate)).ToList();
    }

    private static SettlementDto MapToDto(Settlement settlement, string storeName)
    {
        return new SettlementDto(
            settlement.Id,
            settlement.StoreId,
            settlement.SellerId,
            storeName,
            settlement.Year,
            settlement.Month,
            settlement.SettlementNumber,
            settlement.Status.ToString(),
            settlement.Currency,
            settlement.GrossSales,
            settlement.TotalShipping,
            settlement.TotalCommission,
            settlement.TotalRefunds,
            settlement.TotalAdjustments,
            settlement.NetPayable,
            settlement.OrderCount,
            settlement.Version,
            settlement.PeriodStart,
            settlement.PeriodEnd,
            settlement.CreatedAt,
            settlement.FinalizedAt,
            settlement.ApprovedAt,
            settlement.ExportedAt,
            settlement.ApprovedBy,
            settlement.Notes);
    }

    private static SettlementDetailsDto MapToDetailsDto(Settlement settlement, string storeName)
    {
        var items = settlement.Items.Select(i => new SettlementItemDto(
            i.Id,
            i.EscrowAllocationId,
            i.ShipmentId,
            i.OrderNumber,
            i.SellerAmount,
            i.ShippingAmount,
            i.CommissionAmount,
            i.RefundedAmount,
            i.NetAmount,
            i.TransactionDate)).ToList();

        var adjustments = settlement.Adjustments.Select(a => new SettlementAdjustmentDto(
            a.Id,
            a.OriginalYear,
            a.OriginalMonth,
            a.Amount,
            a.Reason,
            a.RelatedOrderId,
            a.RelatedOrderNumber,
            a.CreatedAt)).ToList();

        return new SettlementDetailsDto(
            settlement.Id,
            settlement.StoreId,
            settlement.SellerId,
            storeName,
            settlement.Year,
            settlement.Month,
            settlement.SettlementNumber,
            settlement.Status.ToString(),
            settlement.Currency,
            settlement.GrossSales,
            settlement.TotalShipping,
            settlement.TotalCommission,
            settlement.TotalRefunds,
            settlement.TotalAdjustments,
            settlement.NetPayable,
            settlement.OrderCount,
            settlement.Version,
            settlement.PeriodStart,
            settlement.PeriodEnd,
            items,
            adjustments,
            settlement.CreatedAt,
            settlement.FinalizedAt,
            settlement.ApprovedAt,
            settlement.ExportedAt,
            settlement.ApprovedBy,
            settlement.Notes);
    }

    private static SettlementListItemDto MapToListItemDto(Settlement settlement, string storeName)
    {
        return new SettlementListItemDto(
            settlement.Id,
            settlement.StoreId,
            storeName,
            settlement.Year,
            settlement.Month,
            settlement.SettlementNumber,
            settlement.Status.ToString(),
            settlement.Currency,
            settlement.NetPayable,
            settlement.OrderCount,
            settlement.Version,
            settlement.CreatedAt,
            settlement.FinalizedAt);
    }
}
