namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO representing a shipping method option.
/// </summary>
public sealed record ShippingMethodDto(
    Guid Id,
    Guid? StoreId,
    string Name,
    string? Description,
    string? CarrierName,
    int EstimatedDeliveryDaysMin,
    int EstimatedDeliveryDaysMax,
    string DeliveryTimeDisplay,
    decimal Cost,
    string Currency,
    bool IsFreeShipping,
    bool IsDefault);

/// <summary>
/// DTO representing shipping methods grouped by seller.
/// </summary>
public sealed record SellerShippingOptionsDto(
    Guid StoreId,
    string StoreName,
    IReadOnlyList<ShippingMethodDto> Methods,
    Guid? SelectedMethodId,
    decimal Subtotal,
    int ItemCount);

/// <summary>
/// DTO for checkout shipping step data.
/// </summary>
public sealed record CheckoutShippingDto(
    IReadOnlyList<SellerShippingOptionsDto> SellerShippingOptions,
    decimal ItemSubtotal,
    decimal TotalShipping,
    decimal TotalAmount,
    string Currency);

/// <summary>
/// DTO representing a payment method option.
/// </summary>
public sealed record PaymentMethodDto(
    Guid Id,
    string Name,
    string? Description,
    string Type,
    string? IconClass,
    bool IsDefault);

/// <summary>
/// DTO for checkout payment step data.
/// </summary>
public sealed record CheckoutPaymentDto(
    IReadOnlyList<PaymentMethodDto> PaymentMethods,
    Guid? SelectedPaymentMethodId,
    decimal ItemSubtotal,
    decimal TotalShipping,
    decimal TotalAmount,
    string Currency,
    string? DeliveryAddressSummary);

/// <summary>
/// Result of selecting shipping methods.
/// </summary>
public sealed record SelectShippingResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    decimal? NewTotalShipping,
    decimal? NewTotalAmount)
{
    public static SelectShippingResultDto Succeeded(decimal totalShipping, decimal totalAmount)
        => new(true, null, totalShipping, totalAmount);

    public static SelectShippingResultDto Failed(string errorMessage)
        => new(false, errorMessage, null, null);
}

/// <summary>
/// Result of initiating payment.
/// </summary>
public sealed record InitiatePaymentResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    Guid? OrderId,
    string? OrderNumber,
    string? PaymentRedirectUrl,
    bool RequiresRedirect)
{
    public static InitiatePaymentResultDto Succeeded(Guid orderId, string orderNumber, string? redirectUrl = null)
        => new(true, null, orderId, orderNumber, redirectUrl, redirectUrl is not null);

    public static InitiatePaymentResultDto Failed(string errorMessage)
        => new(false, errorMessage, null, null, null, false);
}

/// <summary>
/// Result of confirming payment (after return from payment provider).
/// </summary>
public sealed record ConfirmPaymentResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    Guid? OrderId,
    string? OrderNumber)
{
    public static ConfirmPaymentResultDto Succeeded(Guid orderId, string orderNumber)
        => new(true, null, orderId, orderNumber);

    public static ConfirmPaymentResultDto Failed(string errorMessage)
        => new(false, errorMessage, null, null);
}

/// <summary>
/// DTO for order confirmation page.
/// </summary>
public sealed record OrderConfirmationDto(
    Guid OrderId,
    string OrderNumber,
    string Status,
    string RecipientName,
    string DeliveryAddressSummary,
    string PaymentMethodName,
    IReadOnlyList<OrderItemDto> Items,
    decimal ItemSubtotal,
    decimal TotalShipping,
    decimal TotalAmount,
    string Currency,
    DateTime CreatedAt);

/// <summary>
/// DTO for an order item in confirmation.
/// </summary>
public sealed record OrderItemDto(
    Guid ProductId,
    string ProductName,
    Guid StoreId,
    string StoreName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    string? ShippingMethodName,
    decimal ShippingCost);
