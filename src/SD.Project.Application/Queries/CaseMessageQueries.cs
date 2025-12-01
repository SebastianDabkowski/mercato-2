namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get case messages for a specific case.
/// </summary>
public sealed record GetCaseMessagesQuery(
    Guid ReturnRequestId,
    Guid UserId,
    string UserRole);

/// <summary>
/// Query to get unread message count for a buyer across all their cases.
/// </summary>
public sealed record GetBuyerUnreadMessageCountQuery(
    Guid BuyerId);

/// <summary>
/// Query to get unread message count for a store across all cases.
/// </summary>
public sealed record GetStoreUnreadMessageCountQuery(
    Guid StoreId);
