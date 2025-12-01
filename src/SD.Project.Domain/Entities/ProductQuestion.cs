namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents the status of a product question.
/// </summary>
public enum ProductQuestionStatus
{
    /// <summary>Question is pending and awaiting seller response.</summary>
    Pending,
    /// <summary>Question has been answered by the seller.</summary>
    Answered,
    /// <summary>Question has been hidden by a moderator.</summary>
    Hidden
}

/// <summary>
/// Represents a question asked by a buyer about a product.
/// Questions are public Q&amp;A that other buyers can see once answered.
/// </summary>
public class ProductQuestion
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The product this question is about.
    /// </summary>
    public Guid ProductId { get; private set; }

    /// <summary>
    /// The store that owns the product.
    /// </summary>
    public Guid StoreId { get; private set; }

    /// <summary>
    /// The buyer who asked the question.
    /// </summary>
    public Guid BuyerId { get; private set; }

    /// <summary>
    /// Display name of the buyer (for public display).
    /// </summary>
    public string BuyerDisplayName { get; private set; } = default!;

    /// <summary>
    /// The question text.
    /// </summary>
    public string Question { get; private set; } = default!;

    /// <summary>
    /// The seller's answer (if answered).
    /// </summary>
    public string? Answer { get; private set; }

    /// <summary>
    /// Current status of the question.
    /// </summary>
    public ProductQuestionStatus Status { get; private set; }

    /// <summary>
    /// When the question was asked.
    /// </summary>
    public DateTime AskedAt { get; private set; }

    /// <summary>
    /// When the question was answered (if answered).
    /// </summary>
    public DateTime? AnsweredAt { get; private set; }

    /// <summary>
    /// Admin who hid the question (if hidden).
    /// </summary>
    public Guid? HiddenByAdminId { get; private set; }

    /// <summary>
    /// Reason for hiding the question.
    /// </summary>
    public string? HiddenReason { get; private set; }

    /// <summary>
    /// When the question was hidden.
    /// </summary>
    public DateTime? HiddenAt { get; private set; }

    private ProductQuestion()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new product question.
    /// </summary>
    /// <param name="productId">The ID of the product.</param>
    /// <param name="storeId">The ID of the store.</param>
    /// <param name="buyerId">The ID of the buyer asking the question.</param>
    /// <param name="buyerDisplayName">Display name of the buyer.</param>
    /// <param name="question">The question text.</param>
    public ProductQuestion(
        Guid productId,
        Guid storeId,
        Guid buyerId,
        string buyerDisplayName,
        string question)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product ID is required.", nameof(productId));
        }

        if (storeId == Guid.Empty)
        {
            throw new ArgumentException("Store ID is required.", nameof(storeId));
        }

        if (buyerId == Guid.Empty)
        {
            throw new ArgumentException("Buyer ID is required.", nameof(buyerId));
        }

        if (string.IsNullOrWhiteSpace(buyerDisplayName))
        {
            throw new ArgumentException("Buyer display name is required.", nameof(buyerDisplayName));
        }

        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("Question text is required.", nameof(question));
        }

        if (question.Length > 2000)
        {
            throw new ArgumentException("Question cannot exceed 2000 characters.", nameof(question));
        }

        Id = Guid.NewGuid();
        ProductId = productId;
        StoreId = storeId;
        BuyerId = buyerId;
        BuyerDisplayName = buyerDisplayName.Trim();
        Question = question.Trim();
        Status = ProductQuestionStatus.Pending;
        AskedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records the seller's answer to the question.
    /// </summary>
    /// <param name="answer">The answer text.</param>
    public void SetAnswer(string answer)
    {
        if (Status == ProductQuestionStatus.Hidden)
        {
            throw new InvalidOperationException("Cannot answer a hidden question.");
        }

        if (string.IsNullOrWhiteSpace(answer))
        {
            throw new ArgumentException("Answer text is required.", nameof(answer));
        }

        if (answer.Length > 2000)
        {
            throw new ArgumentException("Answer cannot exceed 2000 characters.", nameof(answer));
        }

        Answer = answer.Trim();
        Status = ProductQuestionStatus.Answered;
        AnsweredAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Hides the question (admin moderation).
    /// </summary>
    /// <param name="adminId">The ID of the admin hiding the question.</param>
    /// <param name="reason">The reason for hiding.</param>
    public void Hide(Guid adminId, string reason)
    {
        if (adminId == Guid.Empty)
        {
            throw new ArgumentException("Admin ID is required.", nameof(adminId));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reason is required.", nameof(reason));
        }

        Status = ProductQuestionStatus.Hidden;
        HiddenByAdminId = adminId;
        HiddenReason = reason.Trim();
        HiddenAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Unhides the question (admin action).
    /// </summary>
    public void Unhide()
    {
        if (Status != ProductQuestionStatus.Hidden)
        {
            throw new InvalidOperationException("Question is not hidden.");
        }

        Status = Answer is not null ? ProductQuestionStatus.Answered : ProductQuestionStatus.Pending;
        HiddenByAdminId = null;
        HiddenReason = null;
        HiddenAt = null;
    }
}
