using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Repositories;

/// <summary>
/// Contract for product question persistence operations.
/// </summary>
public interface IProductQuestionRepository
{
    /// <summary>
    /// Gets a question by its ID.
    /// </summary>
    Task<ProductQuestion?> GetByIdAsync(Guid questionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all public (answered and not hidden) questions for a product.
    /// </summary>
    Task<IReadOnlyList<ProductQuestion>> GetPublicQuestionsForProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all questions for a product (including unanswered) - for seller view.
    /// </summary>
    Task<IReadOnlyList<ProductQuestion>> GetAllQuestionsForProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pending questions for a store.
    /// </summary>
    Task<IReadOnlyList<ProductQuestion>> GetPendingQuestionsForStoreAsync(
        Guid storeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all questions for a store (pending and answered, excluding hidden).
    /// </summary>
    Task<IReadOnlyList<ProductQuestion>> GetAllQuestionsForStoreAsync(
        Guid storeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all questions asked by a specific buyer.
    /// </summary>
    Task<IReadOnlyList<ProductQuestion>> GetQuestionsByBuyerAsync(
        Guid buyerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of unanswered questions for a store.
    /// </summary>
    Task<int> GetUnansweredCountForStoreAsync(Guid storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all hidden questions for admin moderation view.
    /// </summary>
    Task<IReadOnlyList<ProductQuestion>> GetHiddenQuestionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new question.
    /// </summary>
    Task AddAsync(ProductQuestion question, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing question.
    /// </summary>
    Task UpdateAsync(ProductQuestion question, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
