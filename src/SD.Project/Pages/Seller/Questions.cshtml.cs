using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;
using System.Security.Claims;

namespace SD.Project.Pages.Seller;

/// <summary>
/// Page model for displaying and managing product questions.
/// </summary>
[RequireRole(UserRole.Seller, UserRole.Admin)]
public class QuestionsModel : PageModel
{
    private readonly ILogger<QuestionsModel> _logger;
    private readonly ProductQuestionService _questionService;
    private readonly StoreService _storeService;

    public string? StoreName { get; private set; }
    public Guid StoreId { get; private set; }
    public IReadOnlyList<ProductQuestionDto> Questions { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; } = "pending";

    public string? Message { get; private set; }
    public bool IsSuccess { get; private set; }

    [BindProperty]
    public AnswerQuestionInputModel AnswerInput { get; set; } = new();

    public QuestionsModel(
        ILogger<QuestionsModel> logger,
        ProductQuestionService questionService,
        StoreService storeService)
    {
        _logger = logger;
        _questionService = questionService;
        _storeService = storeService;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Login");
        }

        // Get seller's store
        var store = await _storeService.HandleAsync(new GetStoreBySellerIdQuery(userId), cancellationToken);
        if (store is null)
        {
            _logger.LogWarning("Store not found for seller {SellerId}", userId);
            return Page();
        }

        StoreName = store.Name;
        StoreId = store.Id;

        // Load questions based on filter
        if (Filter == "pending")
        {
            Questions = await _questionService.HandleAsync(
                new GetPendingQuestionsForStoreQuery(store.Id, userId),
                cancellationToken);
        }
        else
        {
            // Show all questions (pending and answered)
            Questions = await _questionService.HandleAsync(
                new GetAllQuestionsForStoreQuery(store.Id, userId),
                cancellationToken);
        }

        _logger.LogInformation("Seller questions page viewed for store {StoreId}, found {Count} questions",
            store.Id, Questions.Count);

        return Page();
    }

    public async Task<IActionResult> OnPostAnswerAsync(CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Login");
        }

        var result = await _questionService.HandleAsync(
            new AnswerProductQuestionCommand(AnswerInput.QuestionId, userId, AnswerInput.Answer),
            cancellationToken);

        if (result.IsSuccess)
        {
            IsSuccess = true;
            Message = "Your answer has been posted successfully. The buyer will be notified.";
            _logger.LogInformation("Question {QuestionId} answered by seller {SellerId}", AnswerInput.QuestionId, userId);
        }
        else
        {
            IsSuccess = false;
            Message = result.ErrorMessage;
            _logger.LogWarning("Failed to answer question {QuestionId}: {Error}", AnswerInput.QuestionId, result.ErrorMessage);
        }

        return await OnGetAsync(cancellationToken);
    }
}
