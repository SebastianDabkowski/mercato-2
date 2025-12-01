using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for seller rating operations.
/// Handles seller rating submission with order delivery validation.
/// </summary>
public sealed class SellerRatingService
{
    private readonly ISellerRatingRepository _sellerRatingRepository;
    private readonly IOrderRepository _orderRepository;

    public SellerRatingService(
        ISellerRatingRepository sellerRatingRepository,
        IOrderRepository orderRepository)
    {
        ArgumentNullException.ThrowIfNull(sellerRatingRepository);
        ArgumentNullException.ThrowIfNull(orderRepository);

        _sellerRatingRepository = sellerRatingRepository;
        _orderRepository = orderRepository;
    }

    /// <summary>
    /// Checks if a buyer is eligible to submit a seller rating for a specific order.
    /// </summary>
    public async Task<SellerRatingEligibilityDto> HandleAsync(
        CheckSellerRatingEligibilityQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Check if order exists and belongs to the buyer
        var order = await _orderRepository.GetByIdAsync(query.OrderId, cancellationToken);
        if (order is null)
        {
            return new SellerRatingEligibilityDto(false, "Order not found.");
        }

        if (order.BuyerId != query.BuyerId)
        {
            return new SellerRatingEligibilityDto(false, "Order does not belong to this buyer.");
        }

        // Check if order is delivered
        if (order.Status != OrderStatus.Delivered)
        {
            return new SellerRatingEligibilityDto(false, "Seller ratings can only be submitted for delivered orders.");
        }

        // Check if a rating already exists for this order
        var existingRating = await _sellerRatingRepository.GetByOrderIdAsync(query.OrderId, cancellationToken);
        if (existingRating is not null)
        {
            return new SellerRatingEligibilityDto(false, "A seller rating has already been submitted for this order.", true);
        }

        return new SellerRatingEligibilityDto(true);
    }

    /// <summary>
    /// Submits a seller rating.
    /// </summary>
    public async Task<SubmitSellerRatingResultDto> HandleAsync(
        SubmitSellerRatingCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Validate rating
        if (command.Rating < 1 || command.Rating > 5)
        {
            return new SubmitSellerRatingResultDto(false, "Rating must be between 1 and 5.");
        }

        // Check eligibility
        var eligibility = await HandleAsync(
            new CheckSellerRatingEligibilityQuery(command.BuyerId, command.OrderId),
            cancellationToken);

        if (!eligibility.IsEligible)
        {
            return new SubmitSellerRatingResultDto(false, eligibility.IneligibilityReason);
        }

        // Get order to find the store ID
        var order = await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            return new SubmitSellerRatingResultDto(false, "Order not found.");
        }

        // Get the primary store ID from the order shipments
        // For multi-seller orders, we take the first seller's store ID
        // In a more complex scenario, you might want separate ratings per seller
        var storeId = order.Shipments.FirstOrDefault()?.StoreId ?? Guid.Empty;
        if (storeId == Guid.Empty)
        {
            return new SubmitSellerRatingResultDto(false, "Could not determine seller for this order.");
        }

        // Create and save the rating
        var rating = new SellerRating(
            command.OrderId,
            storeId,
            command.BuyerId,
            command.Rating,
            command.Comment);

        await _sellerRatingRepository.AddAsync(rating, cancellationToken);
        await _sellerRatingRepository.SaveChangesAsync(cancellationToken);

        return new SubmitSellerRatingResultDto(true, RatingId: rating.Id);
    }

    /// <summary>
    /// Gets seller rating statistics for a store (contributes to reputation score).
    /// </summary>
    public async Task<SellerRatingStatsDto> HandleAsync(
        GetSellerRatingStatsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (averageRating, ratingCount) = await _sellerRatingRepository.GetStoreRatingStatsAsync(
            query.StoreId, cancellationToken);

        return new SellerRatingStatsDto(query.StoreId, averageRating, ratingCount);
    }
}
