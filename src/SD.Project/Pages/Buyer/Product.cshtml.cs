using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.ViewModels;
using System.Security.Claims;

namespace SD.Project.Pages.Buyer;

/// <summary>
/// Page model for displaying a single product's details.
/// </summary>
public class ProductModel : PageModel
{
    private const int ReviewsPageSize = 5;

    private readonly ILogger<ProductModel> _logger;
    private readonly ProductService _productService;
    private readonly CategoryService _categoryService;
    private readonly StoreService _storeService;
    private readonly CartService _cartService;
    private readonly ReviewService _reviewService;
    private readonly ProductQuestionService _questionService;
    private readonly IAnalyticsService _analyticsService;

    /// <summary>
    /// The product being viewed.
    /// </summary>
    public ProductViewModel? Product { get; private set; }

    /// <summary>
    /// The category ID for navigation (if category exists in the system).
    /// </summary>
    public Guid? CategoryId { get; private set; }

    /// <summary>
    /// The store information for the seller link.
    /// </summary>
    public StoreViewModel? Store { get; private set; }

    /// <summary>
    /// Product rating summary.
    /// </summary>
    public ProductRatingViewModel? Rating { get; private set; }

    /// <summary>
    /// Paginated list of reviews for the product.
    /// </summary>
    public IReadOnlyCollection<ReviewViewModel> Reviews { get; private set; } = Array.Empty<ReviewViewModel>();

    /// <summary>
    /// Current page of reviews (1-based).
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int ReviewPage { get; set; } = 1;

    /// <summary>
    /// Current sort order for reviews.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public ReviewSortOrder ReviewSort { get; set; } = ReviewSortOrder.Newest;

    /// <summary>
    /// Total number of review pages.
    /// </summary>
    public int TotalReviewPages { get; private set; }

    /// <summary>
    /// Total number of reviews.
    /// </summary>
    public int TotalReviewCount { get; private set; }

    /// <summary>
    /// Message to display to the user.
    /// </summary>
    public string? Message { get; private set; }

    /// <summary>
    /// Whether the last operation was successful.
    /// </summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    /// List of product questions and answers.
    /// </summary>
    public IReadOnlyList<ProductQuestionViewModel> Questions { get; private set; } = [];

    /// <summary>
    /// Input model for asking a question.
    /// </summary>
    [BindProperty]
    public AskQuestionInputModel QuestionInput { get; set; } = new();

    public ProductModel(
        ILogger<ProductModel> logger,
        ProductService productService,
        CategoryService categoryService,
        StoreService storeService,
        CartService cartService,
        ReviewService reviewService,
        ProductQuestionService questionService,
        IAnalyticsService analyticsService)
    {
        _logger = logger;
        _productService = productService;
        _categoryService = categoryService;
        _storeService = storeService;
        _cartService = cartService;
        _reviewService = reviewService;
        _questionService = questionService;
        _analyticsService = analyticsService;
    }

    public async Task<IActionResult> OnGetAsync(
        [FromQuery] Guid? id,
        CancellationToken cancellationToken = default)
    {
        if (!id.HasValue)
        {
            _logger.LogWarning("Product page accessed without product ID");
            return Page();
        }

        var productDto = await _productService.HandleAsync(
            new GetProductByIdQuery(id.Value),
            cancellationToken);

        if (productDto is null)
        {
            _logger.LogWarning("Product {ProductId} not found", id.Value);
            return Page();
        }

        // Only show active products to buyers
        if (!productDto.IsActive || productDto.Status != Domain.Entities.ProductStatus.Active)
        {
            _logger.LogWarning("Product {ProductId} is not active (Status: {Status}, IsActive: {IsActive})", 
                id.Value, productDto.Status, productDto.IsActive);
            return Page();
        }

        Product = MapToViewModel(productDto);
        _logger.LogDebug("Loaded product {ProductId}: {ProductName}", id.Value, Product.Name);

        // Track product view analytics event
        var (buyerId, sessionId) = GetCartIdentifiers();
        _ = _analyticsService.TrackProductViewAsync(
            productDto.Id,
            productDto.StoreId,
            buyerId,
            sessionId,
            cancellationToken);

        // Load category ID for navigation link
        if (!string.IsNullOrEmpty(Product.Category))
        {
            await LoadCategoryIdAsync(Product.Category, cancellationToken);
        }

        // Load store info for seller link
        if (productDto.StoreId.HasValue)
        {
            await LoadStoreInfoAsync(productDto.StoreId.Value, cancellationToken);
        }

        // Load reviews and rating
        await LoadReviewsAsync(id.Value, cancellationToken);

        // Load Q&A
        await LoadQuestionsAsync(id.Value, cancellationToken);

        return Page();
    }

    public async Task<IActionResult> OnPostAddToCartAsync(
        Guid productId,
        int quantity = 1,
        CancellationToken cancellationToken = default)
    {
        var (buyerId, sessionId) = GetCartIdentifiers();

        var result = await _cartService.HandleAsync(
            new AddToCartCommand(buyerId, sessionId, productId, quantity),
            cancellationToken);

        if (result.IsSuccess)
        {
            IsSuccess = true;
            if (result.WasQuantityIncreased)
            {
                Message = $"Updated quantity in cart. You now have {result.Item!.Quantity} of this item.";
            }
            else
            {
                Message = "Item added to cart successfully!";
            }
            _logger.LogInformation("Added product {ProductId} to cart (quantity: {Quantity})", productId, quantity);
        }
        else
        {
            IsSuccess = false;
            Message = result.ErrorMessage;
            _logger.LogWarning("Failed to add product {ProductId} to cart: {Error}", productId, result.ErrorMessage);
        }

        // Reload the page with the product
        return await OnGetAsync(productId, cancellationToken);
    }

    private async Task LoadReviewsAsync(Guid productId, CancellationToken cancellationToken)
    {
        // Load rating summary
        var ratingDto = await _reviewService.HandleAsync(
            new GetProductRatingQuery(productId),
            cancellationToken);
        Rating = new ProductRatingViewModel(ratingDto.AverageRating, ratingDto.ReviewCount);

        // Ensure valid page number
        if (ReviewPage < 1)
        {
            ReviewPage = 1;
        }

        // Load paginated reviews
        var pagedReviews = await _reviewService.HandleAsync(
            new GetProductReviewsPagedQuery(productId, ReviewSort, ReviewPage, ReviewsPageSize),
            cancellationToken);

        Reviews = pagedReviews.Items
            .Select(r => new ReviewViewModel(
                r.ReviewId,
                r.ProductId,
                r.BuyerName,
                r.Rating,
                r.Comment,
                r.CreatedAt))
            .ToArray();

        TotalReviewPages = pagedReviews.TotalPages;
        TotalReviewCount = pagedReviews.TotalCount;

        _logger.LogDebug("Loaded {ReviewCount} reviews for product {ProductId} (page {Page} of {TotalPages})",
            Reviews.Count, productId, ReviewPage, TotalReviewPages);
    }

    private (Guid? BuyerId, string? SessionId) GetCartIdentifiers()
    {
        // Check if user is authenticated
        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var buyerId))
            {
                return (buyerId, null);
            }
        }

        // For anonymous users, use session
        var sessionId = HttpContext.Session.GetString(Constants.CartSessionKey);
        if (string.IsNullOrEmpty(sessionId))
        {
            sessionId = Guid.NewGuid().ToString();
            HttpContext.Session.SetString(Constants.CartSessionKey, sessionId);
        }

        return (null, sessionId);
    }

    private async Task LoadCategoryIdAsync(string categoryName, CancellationToken cancellationToken)
    {
        var category = await _categoryService.HandleAsync(new GetCategoryByNameQuery(categoryName), cancellationToken);
        
        if (category is not null)
        {
            CategoryId = category.Id;
            _logger.LogDebug("Found category ID {CategoryId} for category '{CategoryName}'", CategoryId, categoryName);
        }
        else
        {
            _logger.LogDebug("No category found matching '{CategoryName}'", categoryName);
        }
    }

    private async Task LoadStoreInfoAsync(Guid storeId, CancellationToken cancellationToken)
    {
        var storeDto = await _storeService.HandleAsync(new GetStoreByIdQuery(storeId), cancellationToken);
        
        if (storeDto is not null && storeDto.IsPubliclyVisible)
        {
            Store = new StoreViewModel(storeDto.Id, storeDto.Name, storeDto.Slug);
            _logger.LogDebug("Loaded store info for product: {StoreName} ({StoreSlug})", storeDto.Name, storeDto.Slug);
        }
        else
        {
            _logger.LogDebug("Store {StoreId} not found or not publicly visible", storeId);
        }
    }

    private static ProductViewModel MapToViewModel(ProductDto dto)
    {
        return new ProductViewModel(
            dto.Id,
            dto.Name,
            dto.Description,
            dto.Amount,
            dto.Currency,
            dto.Stock,
            dto.Category,
            dto.Status,
            dto.IsActive,
            dto.CreatedAt,
            dto.UpdatedAt,
            dto.WeightKg,
            dto.LengthCm,
            dto.WidthCm,
            dto.HeightCm,
            dto.MainImageUrl,
            dto.MainImageThumbnailUrl);
    }

    public async Task<IActionResult> OnPostAskQuestionAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        // User must be authenticated to ask questions
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToPage("/Login", new { returnUrl = $"/Buyer/Product?id={productId}" });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var buyerId))
        {
            Message = "Please log in to ask a question.";
            IsSuccess = false;
            return await OnGetAsync(productId, cancellationToken);
        }

        var result = await _questionService.HandleAsync(
            new AskProductQuestionCommand(productId, buyerId, QuestionInput.Question),
            cancellationToken);

        if (result.IsSuccess)
        {
            IsSuccess = true;
            Message = "Your question has been submitted. The seller will be notified and you'll receive an email when they respond.";
            QuestionInput = new AskQuestionInputModel(); // Clear the form
            _logger.LogInformation("Question submitted for product {ProductId} by buyer {BuyerId}", productId, buyerId);
        }
        else
        {
            IsSuccess = false;
            Message = result.ErrorMessage;
            _logger.LogWarning("Failed to submit question for product {ProductId}: {Error}", productId, result.ErrorMessage);
        }

        return await OnGetAsync(productId, cancellationToken);
    }

    private async Task LoadQuestionsAsync(Guid productId, CancellationToken cancellationToken)
    {
        var questionsDto = await _questionService.HandleAsync(
            new GetPublicProductQuestionsQuery(productId),
            cancellationToken);

        Questions = questionsDto.Questions.Select(q => new ProductQuestionViewModel(
            q.Id,
            q.ProductId,
            q.ProductName,
            q.BuyerDisplayName,
            q.Question,
            q.Answer,
            q.Status,
            q.AskedAt,
            q.AnsweredAt)).ToList().AsReadOnly();

        _logger.LogDebug("Loaded {QuestionCount} Q&A items for product {ProductId}", Questions.Count, productId);
    }
}
