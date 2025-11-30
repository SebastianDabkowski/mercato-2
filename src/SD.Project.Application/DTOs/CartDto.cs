namespace SD.Project.Application.DTOs;

/// <summary>
/// Represents a shopping cart with items grouped by seller.
/// Includes item subtotal, shipping costs, discount, and total amount payable.
/// </summary>
public sealed record CartDto(
    Guid Id,
    Guid? BuyerId,
    IReadOnlyCollection<CartSellerGroupDto> SellerGroups,
    int TotalItemCount,
    int UniqueItemCount,
    /// <summary>
    /// Sum of all item prices (quantity × unit price) across all sellers.
    /// </summary>
    decimal ItemSubtotal,
    /// <summary>
    /// Total shipping cost across all sellers.
    /// </summary>
    decimal TotalShipping,
    /// <summary>
    /// Discount amount from applied promo code.
    /// </summary>
    decimal DiscountAmount,
    /// <summary>
    /// Total amount payable (item subtotal + total shipping - discount).
    /// </summary>
    decimal TotalAmount,
    /// <summary>
    /// Currency code for all amounts (e.g., "USD", "EUR").
    /// </summary>
    string Currency,
    /// <summary>
    /// Applied promo code information, if any.
    /// </summary>
    AppliedPromoCodeDto? AppliedPromoCode,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
/// Represents cart items grouped by a single seller.
/// Includes item subtotal and shipping cost for this seller.
/// </summary>
public sealed record CartSellerGroupDto(
    Guid StoreId,
    string StoreName,
    string? StoreSlug,
    IReadOnlyCollection<CartItemDto> Items,
    /// <summary>
    /// Sum of item prices for this seller (quantity × unit price).
    /// </summary>
    decimal Subtotal,
    /// <summary>
    /// Shipping cost for this seller.
    /// </summary>
    decimal ShippingCost,
    /// <summary>
    /// Total for this seller (subtotal + shipping).
    /// </summary>
    decimal SellerTotal);

/// <summary>
/// Represents a single item in the cart.
/// </summary>
public sealed record CartItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string? ProductDescription,
    decimal UnitPrice,
    string Currency,
    int Quantity,
    decimal LineTotal,
    int AvailableStock,
    string? ProductImageUrl,
    DateTime AddedAt);
