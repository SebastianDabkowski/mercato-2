namespace SD.Project.Application.Commands;

/// <summary>
/// Command to apply a promo code to the cart.
/// </summary>
public sealed record ApplyPromoCodeCommand(
    Guid? BuyerId,
    string? SessionId,
    string PromoCode);

/// <summary>
/// Command to remove the applied promo code from the cart.
/// </summary>
public sealed record RemovePromoCodeCommand(
    Guid? BuyerId,
    string? SessionId);
