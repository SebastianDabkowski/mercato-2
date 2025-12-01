using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for review-related operations.
/// Handles review submission with order delivery validation and rate limiting.
/// </summary>
public sealed class ReviewService
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IReviewReportRepository _reviewReportRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// Maximum number of reviews a buyer can submit within the rate limit window.
    /// </summary>
    private const int MaxReviewsPerWindow = 10;

    /// <summary>
    /// Time window for rate limiting in hours.
    /// </summary>
    private const int RateLimitWindowHours = 24;

    public ReviewService(
        IReviewRepository reviewRepository,
        IReviewReportRepository reviewReportRepository,
        IOrderRepository orderRepository,
        IUserRepository userRepository)
    {
        ArgumentNullException.ThrowIfNull(reviewRepository);
        ArgumentNullException.ThrowIfNull(reviewReportRepository);
        ArgumentNullException.ThrowIfNull(orderRepository);
        ArgumentNullException.ThrowIfNull(userRepository);

        _reviewRepository = reviewRepository;
        _reviewReportRepository = reviewReportRepository;
        _orderRepository = orderRepository;
        _userRepository = userRepository;
    }

    /// <summary>
    /// Checks if a buyer is eligible to submit a review for a specific order item.
    /// </summary>
    public async Task<ReviewEligibilityDto> HandleAsync(
        CheckReviewEligibilityQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Check if order exists and belongs to the buyer
        var order = await _orderRepository.GetByIdAsync(query.OrderId, cancellationToken);
        if (order is null)
        {
            return new ReviewEligibilityDto(false, "Order not found.");
        }

        if (order.BuyerId != query.BuyerId)
        {
            return new ReviewEligibilityDto(false, "Order does not belong to this buyer.");
        }

        // Check if order is delivered
        if (order.Status != OrderStatus.Delivered)
        {
            return new ReviewEligibilityDto(false, "Reviews can only be submitted for delivered orders.");
        }

        // Check if the shipment exists and is delivered
        var shipment = order.Shipments.FirstOrDefault(s => s.Id == query.ShipmentId);
        if (shipment is null)
        {
            return new ReviewEligibilityDto(false, "Shipment not found in this order.");
        }

        if (shipment.Status != ShipmentStatus.Delivered)
        {
            return new ReviewEligibilityDto(false, "Reviews can only be submitted for delivered shipments.");
        }

        // Check if the product is in this shipment
        var orderItem = order.Items.FirstOrDefault(i => 
            i.ProductId == query.ProductId && i.StoreId == shipment.StoreId);
        if (orderItem is null)
        {
            return new ReviewEligibilityDto(false, "Product not found in this shipment.");
        }

        // Check if a review already exists
        var existingReview = await _reviewRepository.GetByOrderShipmentProductAsync(
            query.OrderId, query.ShipmentId, query.ProductId, cancellationToken);
        if (existingReview is not null)
        {
            return new ReviewEligibilityDto(false, "A review has already been submitted for this item.", true);
        }

        return new ReviewEligibilityDto(true);
    }

    /// <summary>
    /// Submits a product review.
    /// </summary>
    public async Task<SubmitReviewResultDto> HandleAsync(
        SubmitReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Validate rating
        if (command.Rating < 1 || command.Rating > 5)
        {
            return new SubmitReviewResultDto(false, "Rating must be between 1 and 5.");
        }

        // Check eligibility
        var eligibility = await HandleAsync(
            new CheckReviewEligibilityQuery(
                command.BuyerId,
                command.OrderId,
                command.ShipmentId,
                command.ProductId),
            cancellationToken);

        if (!eligibility.IsEligible)
        {
            return new SubmitReviewResultDto(false, eligibility.IneligibilityReason);
        }

        // Rate limiting check
        var windowStart = DateTime.UtcNow.AddHours(-RateLimitWindowHours);
        var recentReviewCount = await _reviewRepository.GetReviewCountByBuyerInWindowAsync(
            command.BuyerId, windowStart, cancellationToken);

        if (recentReviewCount >= MaxReviewsPerWindow)
        {
            return new SubmitReviewResultDto(
                false, 
                $"Rate limit exceeded. You can submit up to {MaxReviewsPerWindow} reviews per {RateLimitWindowHours} hours.");
        }

        // Get order and shipment to find the store ID
        var order = await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            return new SubmitReviewResultDto(false, "Order not found.");
        }

        var shipment = order.Shipments.FirstOrDefault(s => s.Id == command.ShipmentId);
        if (shipment is null)
        {
            return new SubmitReviewResultDto(false, "Shipment not found.");
        }

        // Create and save the review
        var review = new Review(
            command.OrderId,
            command.ShipmentId,
            command.ProductId,
            shipment.StoreId,
            command.BuyerId,
            command.Rating,
            command.Comment);

        await _reviewRepository.AddAsync(review, cancellationToken);
        await _reviewRepository.SaveChangesAsync(cancellationToken);

        return new SubmitReviewResultDto(true, ReviewId: review.Id);
    }

    /// <summary>
    /// Gets approved reviews for a product.
    /// </summary>
    public async Task<IReadOnlyList<ReviewDto>> HandleAsync(
        GetProductReviewsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var reviews = await _reviewRepository.GetByProductIdAsync(query.ProductId, cancellationToken);
        return await MapReviewsToDtosAsync(reviews, cancellationToken);
    }

    /// <summary>
    /// Gets paginated approved reviews for a product with sorting.
    /// </summary>
    public async Task<PagedResultDto<ReviewDto>> HandleAsync(
        GetProductReviewsPagedQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (reviews, totalCount) = await _reviewRepository.GetByProductIdPagedAsync(
            query.ProductId,
            query.SortOrder,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        var reviewDtos = await MapReviewsToDtosAsync(reviews, cancellationToken);

        return PagedResultDto<ReviewDto>.Create(
            reviewDtos.ToList().AsReadOnly(),
            query.PageNumber,
            query.PageSize,
            totalCount);
    }

    /// <summary>
    /// Gets approved reviews for a store.
    /// </summary>
    public async Task<IReadOnlyList<ReviewDto>> HandleAsync(
        GetStoreReviewsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var reviews = await _reviewRepository.GetByStoreIdAsync(query.StoreId, cancellationToken);
        return await MapReviewsToDtosAsync(reviews, cancellationToken);
    }

    /// <summary>
    /// Gets rating summary for a product.
    /// </summary>
    public async Task<ProductRatingDto> HandleAsync(
        GetProductRatingQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (averageRating, reviewCount) = await _reviewRepository.GetProductRatingAsync(
            query.ProductId, cancellationToken);

        return new ProductRatingDto(query.ProductId, averageRating, reviewCount);
    }

    /// <summary>
    /// Gets rating summary for a store.
    /// </summary>
    public async Task<StoreRatingDto> HandleAsync(
        GetStoreRatingQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (averageRating, reviewCount) = await _reviewRepository.GetStoreRatingAsync(
            query.StoreId, cancellationToken);

        return new StoreRatingDto(query.StoreId, averageRating, reviewCount);
    }

    /// <summary>
    /// Reports a review for admin moderation.
    /// Prevents duplicate reports by the same user.
    /// </summary>
    public async Task<ReportReviewResultDto> HandleAsync(
        ReportReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Check if the review exists and is visible
        var review = await _reviewRepository.GetByIdAsync(command.ReviewId, cancellationToken);
        if (review is null)
        {
            return new ReportReviewResultDto(false, "Review not found.");
        }

        if (!review.IsVisible)
        {
            return new ReportReviewResultDto(false, "Review is not visible and cannot be reported.");
        }

        // Check if the user has already reported this review
        var existingReport = await _reviewReportRepository.GetByReviewAndReporterAsync(
            command.ReviewId, command.ReporterId, cancellationToken);
        if (existingReport is not null)
        {
            return new ReportReviewResultDto(false, "You have already reported this review.");
        }

        // Create and save the report
        var report = new ReviewReport(
            command.ReviewId,
            command.ReporterId,
            command.Reason,
            command.Details);

        await _reviewReportRepository.AddAsync(report, cancellationToken);

        // Update the review's report count
        review.Report();
        _reviewRepository.Update(review);

        await _reviewReportRepository.SaveChangesAsync(cancellationToken);

        return new ReportReviewResultDto(true, ReportId: report.Id);
    }

    private async Task<IReadOnlyList<ReviewDto>> MapReviewsToDtosAsync(
        IReadOnlyList<Review> reviews,
        CancellationToken cancellationToken)
    {
        if (reviews.Count == 0)
        {
            return Array.Empty<ReviewDto>();
        }

        // Get buyer names
        var buyerIds = reviews.Select(r => r.BuyerId).Distinct().ToList();
        var buyers = await _userRepository.GetByIdsAsync(buyerIds, cancellationToken);
        var buyerLookup = buyers.ToDictionary(u => u.Id);

        return reviews.Select(r =>
        {
            string? buyerName = null;
            if (buyerLookup.TryGetValue(r.BuyerId, out var buyer))
            {
                buyerName = !string.IsNullOrEmpty(buyer.FirstName) && !string.IsNullOrEmpty(buyer.LastName)
                    ? $"{buyer.FirstName} {buyer.LastName[0]}."
                    : null;
            }

            return new ReviewDto(
                r.Id,
                r.ProductId,
                r.StoreId,
                r.BuyerId,
                buyerName,
                r.Rating,
                r.Comment,
                r.ModerationStatus.ToString(),
                r.CreatedAt);
        }).ToList().AsReadOnly();
    }
}
