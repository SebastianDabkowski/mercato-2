namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get a message thread for an order and store.
/// </summary>
/// <param name="OrderId">The ID of the order.</param>
/// <param name="StoreId">The ID of the store.</param>
/// <param name="UserId">The ID of the user requesting (for authorization).</param>
/// <param name="UserRole">The role of the user (buyer, seller, admin).</param>
public record GetOrderMessageThreadQuery(
    Guid OrderId,
    Guid StoreId,
    Guid UserId,
    string UserRole);

/// <summary>
/// Query to get all message threads for a buyer.
/// </summary>
/// <param name="BuyerId">The ID of the buyer.</param>
public record GetBuyerMessageThreadsQuery(Guid BuyerId);

/// <summary>
/// Query to get all message threads for a store.
/// </summary>
/// <param name="StoreId">The ID of the store.</param>
/// <param name="SellerId">The ID of the seller (for authorization).</param>
public record GetStoreMessageThreadsQuery(
    Guid StoreId,
    Guid SellerId);

/// <summary>
/// Query to get unread message count for a buyer.
/// </summary>
/// <param name="BuyerId">The ID of the buyer.</param>
public record GetBuyerUnreadOrderMessageCountQuery(Guid BuyerId);

/// <summary>
/// Query to get unread message count for a store.
/// </summary>
/// <param name="StoreId">The ID of the store.</param>
public record GetStoreUnreadOrderMessageCountQuery(Guid StoreId);

/// <summary>
/// Query to get hidden messages (admin moderation view).
/// </summary>
/// <param name="AdminId">The ID of the admin requesting.</param>
public record GetHiddenOrderMessagesQuery(Guid AdminId);
