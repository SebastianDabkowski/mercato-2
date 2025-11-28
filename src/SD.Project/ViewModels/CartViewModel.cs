namespace SD.Project.ViewModels;

/// <summary>
/// View model for displaying the shopping cart.
/// </summary>
public sealed record CartViewModel(
    Guid Id,
    IReadOnlyCollection<CartSellerGroupViewModel> SellerGroups,
    int TotalItemCount,
    int UniqueItemCount,
    decimal TotalAmount,
    string Currency);

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
