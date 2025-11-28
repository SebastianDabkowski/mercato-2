namespace SD.Project.Application.Queries;

/// <summary>
/// Query to get available shipping methods for the current checkout.
/// Returns shipping options grouped by seller based on cart contents and delivery address.
/// </summary>
public sealed record GetCheckoutShippingQuery(
    Guid? BuyerId,
    string? SessionId,
    Guid DeliveryAddressId);

/// <summary>
/// Query to get available payment methods for checkout.
/// </summary>
public sealed record GetCheckoutPaymentMethodsQuery(
    Guid? BuyerId,
    string? SessionId,
    Guid DeliveryAddressId,
    IReadOnlyDictionary<Guid, Guid> ShippingMethodsByStore);

/// <summary>
/// Query to get order confirmation details.
/// </summary>
public sealed record GetOrderConfirmationQuery(
    Guid BuyerId,
    Guid OrderId);

/// <summary>
/// Query to get order by order number.
/// </summary>
public sealed record GetOrderByNumberQuery(
    Guid BuyerId,
    string OrderNumber);

/// <summary>
/// Query to get buyer's order history with pagination.
/// </summary>
public sealed record GetBuyerOrdersQuery(
    Guid BuyerId,
    int Skip = 0,
    int Take = 20);
