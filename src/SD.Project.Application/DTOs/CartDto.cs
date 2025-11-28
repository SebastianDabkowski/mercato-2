namespace SD.Project.Application.DTOs;

/// <summary>
/// Represents a shopping cart with items grouped by seller.
/// </summary>
public sealed record CartDto(
    Guid Id,
    Guid? BuyerId,
    IReadOnlyCollection<CartSellerGroupDto> SellerGroups,
    int TotalItemCount,
    int UniqueItemCount,
    decimal TotalAmount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
/// Represents cart items grouped by a single seller.
/// </summary>
public sealed record CartSellerGroupDto(
    Guid StoreId,
    string StoreName,
    string? StoreSlug,
    IReadOnlyCollection<CartItemDto> Items,
    decimal Subtotal);

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
