namespace SD.Project.ViewModels;

/// <summary>
/// View model for displaying the shopping cart.
/// </summary>
public sealed record CartViewModel(
    Guid Id,
    IReadOnlyCollection<CartSellerGroupViewModel> SellerGroups,
    int TotalItemCount,
    int UniqueItemCount,
    decimal ItemSubtotal,
    decimal TotalShipping,
    decimal DiscountAmount,
    decimal TotalAmount,
    string Currency,
    AppliedPromoCodeViewModel? AppliedPromoCode);

/// <summary>
/// View model for cart items grouped by seller.
/// </summary>
public sealed record CartSellerGroupViewModel(
    Guid StoreId,
    string StoreName,
    string? StoreSlug,
    IReadOnlyCollection<CartItemViewModel> Items,
    decimal Subtotal,
    string Currency);

/// <summary>
/// View model for a single cart item.
/// </summary>
public sealed record CartItemViewModel(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string? ProductDescription,
    decimal UnitPrice,
    string Currency,
    int Quantity,
    decimal LineTotal,
    int AvailableStock,
    string? ProductImageUrl);

/// <summary>
/// View model for applied promo code display.
/// </summary>
public sealed record AppliedPromoCodeViewModel(
    Guid PromoCodeId,
    string PromoCode,
    decimal DiscountAmount,
    string DiscountDescription);
