using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for seller reputation operations.
/// Handles reputation calculation, retrieval, and batch processing.
/// </summary>
public sealed class SellerReputationService
{
    private readonly ISellerReputationRepository _reputationRepository;
    private readonly ISellerRatingRepository _ratingRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IReturnRequestRepository _returnRequestRepository;
    private readonly IStoreRepository _storeRepository;

    public SellerReputationService(
        ISellerReputationRepository reputationRepository,
        ISellerRatingRepository ratingRepository,
        IOrderRepository orderRepository,
        IReturnRequestRepository returnRequestRepository,
        IStoreRepository storeRepository)
    {
        ArgumentNullException.ThrowIfNull(reputationRepository);
        ArgumentNullException.ThrowIfNull(ratingRepository);
        ArgumentNullException.ThrowIfNull(orderRepository);
        ArgumentNullException.ThrowIfNull(returnRequestRepository);
        ArgumentNullException.ThrowIfNull(storeRepository);

        _reputationRepository = reputationRepository;
        _ratingRepository = ratingRepository;
        _orderRepository = orderRepository;
        _returnRequestRepository = returnRequestRepository;
        _storeRepository = storeRepository;
    }

    /// <summary>
    /// Gets the full reputation details for a store.
    /// </summary>
    public async Task<SellerReputationDto?> HandleAsync(
        GetSellerReputationQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var reputation = await _reputationRepository.GetByStoreIdAsync(query.StoreId, cancellationToken);
        if (reputation is null)
        {
            return null;
        }

        return MapToDto(reputation);
    }

    /// <summary>
    /// Gets the simplified reputation summary for buyer display.
    /// </summary>
    public async Task<SellerReputationSummaryDto?> HandleAsync(
        GetSellerReputationSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var reputation = await _reputationRepository.GetByStoreIdAsync(query.StoreId, cancellationToken);
        if (reputation is null)
        {
            return null;
        }

        return MapToSummaryDto(reputation);
    }

    /// <summary>
    /// Gets reputation summaries for multiple stores.
    /// </summary>
    public async Task<IReadOnlyList<SellerReputationSummaryDto>> HandleAsync(
        GetSellerReputationSummariesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var storeIds = query.StoreIds.ToList();
        if (storeIds.Count == 0)
        {
            return Array.Empty<SellerReputationSummaryDto>();
        }

        var reputations = await _reputationRepository.GetByStoreIdsAsync(storeIds, cancellationToken);
        return reputations.Select(MapToSummaryDto).ToList();
    }

    /// <summary>
    /// Gets top sellers by reputation score.
    /// </summary>
    public async Task<IReadOnlyList<SellerReputationSummaryDto>> HandleAsync(
        GetTopSellersByReputationQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var take = Math.Max(1, Math.Min(query.Take, 100));
        var reputations = await _reputationRepository.GetTopSellersAsync(take, cancellationToken);
        return reputations.Select(MapToSummaryDto).ToList();
    }

    /// <summary>
    /// Recalculates the reputation score for a single store.
    /// </summary>
    public async Task<RecalculateReputationResultDto> HandleAsync(
        RecalculateSellerReputationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Verify store exists
        var store = await _storeRepository.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null)
        {
            return new RecalculateReputationResultDto(false, "Store not found.");
        }

        // Get or create reputation record
        var existingReputation = await _reputationRepository.GetByStoreIdAsync(command.StoreId, cancellationToken);
        var isNew = existingReputation is null;

        var reputation = existingReputation ?? new SellerReputation(command.StoreId);

        // Gather metrics from various sources
        var (averageRating, ratingCount) = await _ratingRepository.GetStoreRatingStatsAsync(
            command.StoreId, cancellationToken);

        var (deliveredCount, cancelledCount, onTimeCount, totalShipments) =
            await _orderRepository.GetStoreShipmentStatsAsync(command.StoreId, cancellationToken);

        var disputeCount = await _returnRequestRepository.GetDisputeCountByStoreIdAsync(
            command.StoreId, cancellationToken);

        // Update metrics
        // Note: For reputation purposes, "completed orders" = delivered shipments
        // (successfully fulfilled orders that reached the customer)
        reputation.UpdateMetrics(
            averageRating: (decimal)averageRating,
            totalRatings: ratingCount,
            totalCompletedOrders: deliveredCount,
            totalCancelledOrders: cancelledCount,
            totalDisputes: disputeCount,
            onTimeShipments: onTimeCount,
            totalShipments: totalShipments);

        // Persist
        if (isNew)
        {
            await _reputationRepository.AddAsync(reputation, cancellationToken);
        }
        else
        {
            _reputationRepository.Update(reputation);
        }

        await _reputationRepository.SaveChangesAsync(cancellationToken);

        return new RecalculateReputationResultDto(true, Reputation: MapToDto(reputation));
    }

    /// <summary>
    /// Batch recalculates reputation scores for stale entries.
    /// </summary>
    public async Task<BatchRecalculateReputationResultDto> HandleAsync(
        BatchRecalculateReputationsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var olderThan = DateTime.UtcNow - command.StaleThreshold;
        var batchSize = Math.Max(1, Math.Min(command.BatchSize, 1000));

        var staleReputations = await _reputationRepository.GetStaleReputationsAsync(
            olderThan, batchSize, cancellationToken);

        var successCount = 0;
        var errors = new List<string>();

        foreach (var reputation in staleReputations)
        {
            try
            {
                var result = await HandleAsync(
                    new RecalculateSellerReputationCommand(reputation.StoreId),
                    cancellationToken);

                if (result.Success)
                {
                    successCount++;
                }
                else
                {
                    errors.Add($"Store {reputation.StoreId}: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Store {reputation.StoreId}: {ex.Message}");
            }
        }

        return new BatchRecalculateReputationResultDto(
            TotalProcessed: staleReputations.Count,
            SuccessCount: successCount,
            FailureCount: errors.Count,
            Errors: errors);
    }

    private static SellerReputationDto MapToDto(SellerReputation reputation)
    {
        return new SellerReputationDto(
            StoreId: reputation.StoreId,
            ReputationScore: reputation.ReputationScore,
            ReputationTier: reputation.GetReputationTier(),
            AverageRating: reputation.AverageRating,
            TotalRatings: reputation.TotalRatings,
            OnTimeShippingRate: reputation.OnTimeShippingRate,
            CancellationRate: reputation.CancellationRate,
            DisputeRate: reputation.DisputeRate,
            LastCalculatedAt: reputation.LastCalculatedAt);
    }

    private static SellerReputationSummaryDto MapToSummaryDto(SellerReputation reputation)
    {
        return new SellerReputationSummaryDto(
            StoreId: reputation.StoreId,
            ReputationScore: reputation.ReputationScore,
            ReputationTier: reputation.GetReputationTier(),
            AverageRating: reputation.AverageRating,
            TotalRatings: reputation.TotalRatings,
            OnTimeShippingRate: reputation.OnTimeShippingRate);
    }
}
