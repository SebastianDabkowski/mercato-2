namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get public questions for a product (answered and visible).
/// </summary>
/// <param name="ProductId">The ID of the product.</param>
public record GetPublicProductQuestionsQuery(Guid ProductId);

/// <summary>
/// Query to get all questions for a product (seller view).
/// </summary>
/// <param name="ProductId">The ID of the product.</param>
/// <param name="SellerId">The ID of the seller requesting (for authorization).</param>
public record GetAllProductQuestionsQuery(
    Guid ProductId,
    Guid SellerId);

/// <summary>
/// Query to get pending questions for a store.
/// </summary>
/// <param name="StoreId">The ID of the store.</param>
/// <param name="SellerId">The ID of the seller requesting (for authorization).</param>
public record GetPendingQuestionsForStoreQuery(
    Guid StoreId,
    Guid SellerId);

/// <summary>
/// Query to get all questions for a store (pending and answered).
/// </summary>
/// <param name="StoreId">The ID of the store.</param>
/// <param name="SellerId">The ID of the seller requesting (for authorization).</param>
public record GetAllQuestionsForStoreQuery(
    Guid StoreId,
    Guid SellerId);

/// <summary>
/// Query to get questions asked by a buyer.
/// </summary>
/// <param name="BuyerId">The ID of the buyer.</param>
public record GetBuyerQuestionsQuery(Guid BuyerId);

/// <summary>
/// Query to get count of unanswered questions for a store.
/// </summary>
/// <param name="StoreId">The ID of the store.</param>
public record GetUnansweredQuestionCountQuery(Guid StoreId);

/// <summary>
/// Query to get hidden questions (admin moderation view).
/// </summary>
/// <param name="AdminId">The ID of the admin requesting.</param>
public record GetHiddenQuestionsQuery(Guid AdminId);
