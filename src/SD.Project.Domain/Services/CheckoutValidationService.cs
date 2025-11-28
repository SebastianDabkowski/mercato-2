using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Services;

/// <summary>
/// Result of a single cart item validation.
/// </summary>
public sealed class CartItemValidationResult
{
    public Guid ProductId { get; }
    public string ProductName { get; }
    public bool IsStockValid { get; }
    public bool IsPriceValid { get; }
    public int RequestedQuantity { get; }
    public int AvailableStock { get; }
    public decimal OriginalPrice { get; }
    public decimal CurrentPrice { get; }
    public string Currency { get; }
    public string? StockValidationMessage { get; }
    public string? PriceValidationMessage { get; }

    private CartItemValidationResult(
        Guid productId,
        string productName,
        bool isStockValid,
        bool isPriceValid,
        int requestedQuantity,
        int availableStock,
        decimal originalPrice,
        decimal currentPrice,
        string currency,
        string? stockValidationMessage,
        string? priceValidationMessage)
    {
        ProductId = productId;
        ProductName = productName;
        IsStockValid = isStockValid;
        IsPriceValid = isPriceValid;
        RequestedQuantity = requestedQuantity;
        AvailableStock = availableStock;
        OriginalPrice = originalPrice;
        CurrentPrice = currentPrice;
        Currency = currency;
        StockValidationMessage = stockValidationMessage;
        PriceValidationMessage = priceValidationMessage;
    }

    public bool IsValid => IsStockValid && IsPriceValid;

    public static CartItemValidationResult Valid(
        Guid productId,
        string productName,
        int requestedQuantity,
        int availableStock,
        decimal price,
        string currency)
    {
        return new CartItemValidationResult(
            productId, productName, true, true,
            requestedQuantity, availableStock,
            price, price, currency,
            null, null);
    }

    public static CartItemValidationResult InsufficientStock(
        Guid productId,
        string productName,
        int requestedQuantity,
        int availableStock,
        decimal price,
        string currency)
    {
        var message = availableStock == 0
            ? $"'{productName}' is currently out of stock."
            : $"'{productName}' has only {availableStock} items available, but you requested {requestedQuantity}.";

        return new CartItemValidationResult(
            productId, productName, false, true,
            requestedQuantity, availableStock,
            price, price, currency,
            message, null);
    }

    public static CartItemValidationResult PriceChanged(
        Guid productId,
        string productName,
        int requestedQuantity,
        int availableStock,
        decimal originalPrice,
        decimal currentPrice,
        string currency)
    {
        var message = FormatPriceChangeMessage(productName, originalPrice, currentPrice);

        return new CartItemValidationResult(
            productId, productName, true, false,
            requestedQuantity, availableStock,
            originalPrice, currentPrice, currency,
            null, message);
    }

    public static CartItemValidationResult StockAndPriceIssues(
        Guid productId,
        string productName,
        int requestedQuantity,
        int availableStock,
        decimal originalPrice,
        decimal currentPrice,
        string currency)
    {
        var stockMessage = availableStock == 0
            ? $"'{productName}' is currently out of stock."
            : $"'{productName}' has only {availableStock} items available, but you requested {requestedQuantity}.";

        var priceMessage = FormatPriceChangeMessage(productName, originalPrice, currentPrice);

        return new CartItemValidationResult(
            productId, productName, false, false,
            requestedQuantity, availableStock,
            originalPrice, currentPrice, currency,
            stockMessage, priceMessage);
    }

    private static string FormatPriceChangeMessage(string productName, decimal originalPrice, decimal currentPrice)
    {
        var direction = currentPrice > originalPrice ? "increased" : "decreased";
        return $"The price of '{productName}' has {direction} from {originalPrice:C} to {currentPrice:C}.";
    }

    public static CartItemValidationResult ProductNotFound(
        Guid productId,
        string productName,
        int requestedQuantity,
        string currency)
    {
        return new CartItemValidationResult(
            productId, productName, false, true,
            requestedQuantity, 0,
            0m, 0m, currency,
            $"'{productName}' is no longer available.", null);
    }

    public static CartItemValidationResult ProductInactive(
        Guid productId,
        string productName,
        int requestedQuantity,
        string currency)
    {
        return new CartItemValidationResult(
            productId, productName, false, true,
            requestedQuantity, 0,
            0m, 0m, currency,
            $"'{productName}' is no longer available for purchase.", null);
    }
}

/// <summary>
/// Result of validating all cart items for checkout.
/// </summary>
public sealed class CheckoutValidationResult
{
    private readonly List<CartItemValidationResult> _itemResults;

    public IReadOnlyList<CartItemValidationResult> ItemResults => _itemResults.AsReadOnly();

    public bool IsValid => _itemResults.All(r => r.IsValid);

    public bool HasStockIssues => _itemResults.Any(r => !r.IsStockValid);

    public bool HasPriceChanges => _itemResults.Any(r => !r.IsPriceValid);

    public IReadOnlyList<CartItemValidationResult> StockIssues =>
        _itemResults.Where(r => !r.IsStockValid).ToList().AsReadOnly();

    public IReadOnlyList<CartItemValidationResult> PriceChanges =>
        _itemResults.Where(r => !r.IsPriceValid).ToList().AsReadOnly();

    public CheckoutValidationResult()
    {
        _itemResults = new List<CartItemValidationResult>();
    }

    public void AddResult(CartItemValidationResult result)
    {
        _itemResults.Add(result);
    }

    /// <summary>
    /// Gets a summary message describing all validation issues.
    /// </summary>
    public string GetSummaryMessage()
    {
        var messages = new List<string>();

        if (HasStockIssues)
        {
            var stockMessages = StockIssues
                .Where(r => r.StockValidationMessage is not null)
                .Select(r => r.StockValidationMessage!);
            messages.AddRange(stockMessages);
        }

        if (HasPriceChanges)
        {
            var priceMessages = PriceChanges
                .Where(r => r.PriceValidationMessage is not null)
                .Select(r => r.PriceValidationMessage!);
            messages.AddRange(priceMessages);
        }

        return messages.Count > 0
            ? string.Join(" ", messages)
            : "Validation passed.";
    }
}

/// <summary>
/// Domain service for validating cart items before checkout.
/// Validates stock availability and detects price changes.
/// </summary>
public sealed class CheckoutValidationService
{
    /// <summary>
    /// Validates cart items against current product state.
    /// </summary>
    /// <param name="cartItems">The cart items to validate.</param>
    /// <param name="products">The current state of products indexed by product ID.</param>
    /// <returns>Validation result containing any stock or price issues.</returns>
    public CheckoutValidationResult ValidateCartItems(
        IReadOnlyCollection<CartItem> cartItems,
        IReadOnlyDictionary<Guid, Product> products)
    {
        var result = new CheckoutValidationResult();

        foreach (var item in cartItems)
        {
            var itemResult = ValidateCartItem(item, products);
            result.AddResult(itemResult);
        }

        return result;
    }

    private CartItemValidationResult ValidateCartItem(
        CartItem item,
        IReadOnlyDictionary<Guid, Product> products)
    {
        // Check if product still exists
        if (!products.TryGetValue(item.ProductId, out var product))
        {
            return CartItemValidationResult.ProductNotFound(
                item.ProductId,
                "Unknown Product",
                item.Quantity,
                item.CurrencyAtAddition);
        }

        // Check if product is still active
        // Both checks are included for defensive programming - Status is the source of truth
        // but IsActive provides a quick boolean check. They should be aligned per Product entity logic.
        if (product.Status != ProductStatus.Active || !product.IsActive)
        {
            return CartItemValidationResult.ProductInactive(
                item.ProductId,
                product.Name,
                item.Quantity,
                item.CurrencyAtAddition);
        }

        var currentPrice = product.Price.Amount;
        var currentCurrency = product.Price.Currency;
        var originalPrice = item.UnitPriceAtAddition;

        var hasStockIssue = product.Stock < item.Quantity;
        var hasPriceChange = currentPrice != originalPrice;

        if (hasStockIssue && hasPriceChange)
        {
            return CartItemValidationResult.StockAndPriceIssues(
                item.ProductId,
                product.Name,
                item.Quantity,
                product.Stock,
                originalPrice,
                currentPrice,
                currentCurrency);
        }

        if (hasStockIssue)
        {
            return CartItemValidationResult.InsufficientStock(
                item.ProductId,
                product.Name,
                item.Quantity,
                product.Stock,
                currentPrice,
                currentCurrency);
        }

        if (hasPriceChange)
        {
            return CartItemValidationResult.PriceChanged(
                item.ProductId,
                product.Name,
                item.Quantity,
                product.Stock,
                originalPrice,
                currentPrice,
                currentCurrency);
        }

        return CartItemValidationResult.Valid(
            item.ProductId,
            product.Name,
            item.Quantity,
            product.Stock,
            currentPrice,
            currentCurrency);
    }
}
