using SD.Project.Domain.Entities;

namespace SD.Project.Application.DTOs;

/// <summary>
/// Data transfer object for a product question.
/// </summary>
/// <param name="Id">The question ID.</param>
/// <param name="ProductId">The product ID.</param>
/// <param name="ProductName">The product name.</param>
/// <param name="StoreId">The store ID.</param>
/// <param name="BuyerId">The buyer ID.</param>
/// <param name="BuyerDisplayName">Display name of the buyer.</param>
/// <param name="Question">The question text.</param>
/// <param name="Answer">The answer text (if answered).</param>
/// <param name="Status">The question status.</param>
/// <param name="AskedAt">When the question was asked.</param>
/// <param name="AnsweredAt">When the question was answered (if answered).</param>
public record ProductQuestionDto(
    Guid Id,
    Guid ProductId,
    string? ProductName,
    Guid StoreId,
    Guid BuyerId,
    string BuyerDisplayName,
    string Question,
    string? Answer,
    ProductQuestionStatus Status,
    DateTime AskedAt,
    DateTime? AnsweredAt);

/// <summary>
/// Data transfer object for a list of product questions for a product.
/// </summary>
/// <param name="ProductId">The product ID.</param>
/// <param name="ProductName">The product name.</param>
/// <param name="Questions">The list of questions.</param>
/// <param name="TotalCount">Total number of questions.</param>
public record ProductQuestionsListDto(
    Guid ProductId,
    string ProductName,
    IReadOnlyList<ProductQuestionDto> Questions,
    int TotalCount);

/// <summary>
/// Result of asking a product question.
/// </summary>
/// <param name="IsSuccess">Whether the operation succeeded.</param>
/// <param name="ErrorMessage">Error message if failed.</param>
/// <param name="QuestionId">The created question ID if successful.</param>
public record AskProductQuestionResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    Guid? QuestionId = null);

/// <summary>
/// Result of answering a product question.
/// </summary>
/// <param name="IsSuccess">Whether the operation succeeded.</param>
/// <param name="ErrorMessage">Error message if failed.</param>
public record AnswerProductQuestionResultDto(
    bool IsSuccess,
    string? ErrorMessage);

/// <summary>
/// Result of hiding or unhiding a product question.
/// </summary>
/// <param name="IsSuccess">Whether the operation succeeded.</param>
/// <param name="ErrorMessage">Error message if failed.</param>
public record ModerateProductQuestionResultDto(
    bool IsSuccess,
    string? ErrorMessage);
