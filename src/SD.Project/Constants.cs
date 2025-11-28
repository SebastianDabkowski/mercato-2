namespace SD.Project;

/// <summary>
/// Application-wide constants.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Session key for storing the anonymous cart identifier.
    /// </summary>
    public const string CartSessionKey = "CartSessionId";

    /// <summary>
    /// Session key for storing the selected checkout address ID.
    /// </summary>
    public const string CheckoutAddressIdKey = "CheckoutAddressId";

    /// <summary>
    /// Session key for storing selected shipping methods by store (JSON).
    /// </summary>
    public const string CheckoutShippingMethodsKey = "CheckoutShippingMethods";

    /// <summary>
    /// Session key for storing the selected payment method ID.
    /// </summary>
    public const string CheckoutPaymentMethodIdKey = "CheckoutPaymentMethodId";
}
