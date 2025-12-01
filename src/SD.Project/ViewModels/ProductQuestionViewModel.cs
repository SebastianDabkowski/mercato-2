using SD.Project.Domain.Entities;

namespace SD.Project.ViewModels;

/// <summary>
/// View model for displaying a product question.
/// </summary>
public record ProductQuestionViewModel(
    Guid Id,
    Guid ProductId,
    string? ProductName,
    string BuyerDisplayName,
    string Question,
    string? Answer,
    ProductQuestionStatus Status,
    DateTime AskedAt,
    DateTime? AnsweredAt)
{
    /// <summary>
    /// Gets the formatted date when the question was asked.
    /// </summary>
    public string AskedAtDisplay => AskedAt.ToString("MMM d, yyyy");

    /// <summary>
    /// Gets the formatted date when the question was answered.
    /// </summary>
    public string? AnsweredAtDisplay => AnsweredAt?.ToString("MMM d, yyyy");

    /// <summary>
    /// Gets whether the question has been answered.
    /// </summary>
    public bool IsAnswered => Status == ProductQuestionStatus.Answered;
}

/// <summary>
/// View model for the question form input.
/// </summary>
public class AskQuestionInputModel
{
    public Guid ProductId { get; set; }
    public string Question { get; set; } = string.Empty;
}

/// <summary>
/// View model for answering a question.
/// </summary>
public class AnswerQuestionInputModel
{
    public Guid QuestionId { get; set; }
    public string Answer { get; set; } = string.Empty;
}
