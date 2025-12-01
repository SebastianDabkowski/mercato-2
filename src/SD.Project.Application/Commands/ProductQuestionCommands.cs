namespace SD.Project.Application.Commands;

/// <summary>
/// Command to ask a question about a product.
/// </summary>
/// <param name="ProductId">The ID of the product.</param>
/// <param name="BuyerId">The ID of the buyer asking the question.</param>
/// <param name="Question">The question text.</param>
public record AskProductQuestionCommand(
    Guid ProductId,
    Guid BuyerId,
    string Question);

/// <summary>
/// Command to answer a product question.
/// </summary>
/// <param name="QuestionId">The ID of the question to answer.</param>
/// <param name="SellerId">The ID of the seller answering.</param>
/// <param name="Answer">The answer text.</param>
public record AnswerProductQuestionCommand(
    Guid QuestionId,
    Guid SellerId,
    string Answer);

/// <summary>
/// Command to hide a product question (admin moderation).
/// </summary>
/// <param name="QuestionId">The ID of the question to hide.</param>
/// <param name="AdminId">The ID of the admin performing the action.</param>
/// <param name="Reason">The reason for hiding.</param>
public record HideProductQuestionCommand(
    Guid QuestionId,
    Guid AdminId,
    string Reason);

/// <summary>
/// Command to unhide a product question (admin moderation).
/// </summary>
/// <param name="QuestionId">The ID of the question to unhide.</param>
/// <param name="AdminId">The ID of the admin performing the action.</param>
public record UnhideProductQuestionCommand(
    Guid QuestionId,
    Guid AdminId);
