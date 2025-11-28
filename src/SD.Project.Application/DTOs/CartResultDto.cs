namespace SD.Project.Application.DTOs;

/// <summary>
/// Result of adding an item to the cart.
/// </summary>
public sealed class AddToCartResultDto
{
    public bool IsSuccess { get; private init; }
    public string? ErrorMessage { get; private init; }
    public CartItemDto? Item { get; private init; }
    public bool WasQuantityIncreased { get; private init; }

    private AddToCartResultDto() { }

    public static AddToCartResultDto Succeeded(CartItemDto item, bool wasQuantityIncreased)
    {
        return new AddToCartResultDto
        {
            IsSuccess = true,
            Item = item,
            WasQuantityIncreased = wasQuantityIncreased
        };
    }

    public static AddToCartResultDto Failed(string error)
    {
        return new AddToCartResultDto
        {
            IsSuccess = false,
            ErrorMessage = error
        };
    }
}

/// <summary>
/// Result of updating cart item quantity.
/// </summary>
public sealed class UpdateCartItemResultDto
{
    public bool IsSuccess { get; private init; }
    public string? ErrorMessage { get; private init; }
    public CartItemDto? Item { get; private init; }

    private UpdateCartItemResultDto() { }

    public static UpdateCartItemResultDto Succeeded(CartItemDto item)
    {
        return new UpdateCartItemResultDto
        {
            IsSuccess = true,
            Item = item
        };
    }

    public static UpdateCartItemResultDto Failed(string error)
    {
        return new UpdateCartItemResultDto
        {
            IsSuccess = false,
            ErrorMessage = error
        };
    }
}

/// <summary>
/// Result of removing an item from the cart.
/// </summary>
public sealed class RemoveFromCartResultDto
{
    public bool IsSuccess { get; private init; }
    public string? ErrorMessage { get; private init; }

    private RemoveFromCartResultDto() { }

    public static RemoveFromCartResultDto Succeeded()
    {
        return new RemoveFromCartResultDto
        {
            IsSuccess = true
        };
    }

    public static RemoveFromCartResultDto Failed(string error)
    {
        return new RemoveFromCartResultDto
        {
            IsSuccess = false,
            ErrorMessage = error
        };
    }
}
