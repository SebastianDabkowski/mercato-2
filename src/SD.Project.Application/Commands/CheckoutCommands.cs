namespace SD.Project.Application.Commands;

/// <summary>
/// Command to select shipping methods for checkout.
/// Contains a dictionary of store ID to selected shipping method ID.
/// </summary>
public sealed record SelectShippingMethodsCommand(
    Guid? BuyerId,
    string? SessionId,
    IReadOnlyDictionary<Guid, Guid> ShippingMethodsByStore);

/// <summary>
/// Command to initiate payment during checkout.
/// Creates the order and initiates payment processing.
/// </summary>
public sealed record InitiatePaymentCommand(
    Guid BuyerId,
    Guid DeliveryAddressId,
    Guid PaymentMethodId,
    IReadOnlyDictionary<Guid, Guid> ShippingMethodsByStore);

/// <summary>
/// Command to confirm payment after return from payment provider.
/// </summary>
public sealed record ConfirmPaymentCommand(
    Guid OrderId,
    string? TransactionId,
    bool IsSuccessful);

/// <summary>
/// Command to cancel/retry a failed payment.
/// </summary>
public sealed record RetryPaymentCommand(
    Guid OrderId,
    Guid PaymentMethodId);
