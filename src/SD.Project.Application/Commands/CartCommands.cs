namespace SD.Project.Application.Commands;

/// <summary>
/// Command to add a product to the cart.
/// </summary>
public sealed record AddToCartCommand(
    Guid? BuyerId,
    string? SessionId,
    Guid ProductId,
    int Quantity = 1);

/// <summary>
/// Command to update the quantity of an item in the cart.
/// </summary>
public sealed record UpdateCartItemQuantityCommand(
    Guid? BuyerId,
    string? SessionId,
    Guid ProductId,
    int Quantity);

/// <summary>
/// Command to remove an item from the cart.
/// </summary>
public sealed record RemoveFromCartCommand(
    Guid? BuyerId,
    string? SessionId,
    Guid ProductId);

/// <summary>
/// Command to clear all items from the cart.
/// </summary>
public sealed record ClearCartCommand(
    Guid? BuyerId,
    string? SessionId);

/// <summary>
/// Command to merge an anonymous cart into a buyer's cart upon login.
/// </summary>
public sealed record MergeCartsCommand(
    Guid BuyerId,
    string SessionId);
