namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get the current cart for a buyer or session.
/// </summary>
public sealed record GetCartQuery(
    Guid? BuyerId,
    string? SessionId);

/// <summary>
/// Query to get the item count in the cart.
/// </summary>
public sealed record GetCartItemCountQuery(
    Guid? BuyerId,
    string? SessionId);
