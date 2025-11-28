using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using System.Security.Claims;
using System.Text.Json;

namespace SD.Project.Pages.Buyer;

/// <summary>
/// Page model for the checkout payment step.
/// </summary>
public class CheckoutPaymentModel : PageModel
{
    private readonly ILogger<CheckoutPaymentModel> _logger;
    private readonly CheckoutService _checkoutService;

    public CheckoutPaymentDto? PaymentData { get; private set; }
    public Guid? SelectedPaymentMethodId { get; private set; }
    public bool IsAuthenticated => User.Identity?.IsAuthenticated == true;
    public string? Message { get; private set; }
    public bool IsSuccess { get; private set; }

    /// <summary>
    /// Indicates if the last order attempt had validation issues.
    /// </summary>
    public bool HasValidationIssues { get; private set; }

    /// <summary>
    /// Indicates if there were stock availability issues.
    /// </summary>
    public bool HasStockIssues { get; private set; }

    /// <summary>
    /// Indicates if there were price change issues.
    /// </summary>
    public bool HasPriceChanges { get; private set; }

    /// <summary>
    /// List of validation issues for display.
    /// </summary>
    public IReadOnlyList<CartItemValidationIssueDto>? ValidationIssues { get; private set; }

    public CheckoutPaymentModel(
        ILogger<CheckoutPaymentModel> logger,
        CheckoutService checkoutService)
    {
        _logger = logger;
        _checkoutService = checkoutService;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
    {
        // Verify user is authenticated
        if (!IsAuthenticated)
        {
            return Page();
        }

        // Verify checkout data exists
        var addressId = GetSelectedAddressId();
        var shippingMethods = GetSelectedShippingMethods();

        if (!addressId.HasValue || shippingMethods.Count == 0)
        {
            return RedirectToPage("/Buyer/CheckoutShipping");
        }

        await LoadPageDataAsync(addressId.Value, shippingMethods, cancellationToken);

        if (PaymentData is null)
        {
            return RedirectToPage("/Buyer/Cart");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostPlaceOrderAsync(
        Guid selectedPaymentMethodId,
        bool acceptTerms,
        CancellationToken cancellationToken = default)
    {
        // Verify user is authenticated
        if (!IsAuthenticated)
        {
            return RedirectToPage("/Login", new { returnUrl = "/Buyer/CheckoutPayment" });
        }

        // Verify terms accepted
        if (!acceptTerms)
        {
            IsSuccess = false;
            Message = "Please accept the terms and conditions to continue.";
            await LoadPageDataForErrorAsync(cancellationToken);
            return Page();
        }

        var buyerId = GetBuyerId();
        if (!buyerId.HasValue)
        {
            return RedirectToPage("/Login", new { returnUrl = "/Buyer/CheckoutPayment" });
        }

        var addressId = GetSelectedAddressId();
        var shippingMethods = GetSelectedShippingMethods();

        if (!addressId.HasValue || shippingMethods.Count == 0)
        {
            return RedirectToPage("/Buyer/CheckoutShipping");
        }

        // Initiate payment and create order
        var result = await _checkoutService.HandleAsync(
            new InitiatePaymentCommand(
                buyerId.Value,
                addressId.Value,
                selectedPaymentMethodId,
                shippingMethods),
            cancellationToken);

        if (!result.IsSuccess)
        {
            IsSuccess = false;
            Message = result.ErrorMessage;

            // Capture validation issues for display
            HasValidationIssues = result.HasValidationIssues;
            HasStockIssues = result.HasStockIssues;
            HasPriceChanges = result.HasPriceChanges;
            ValidationIssues = result.ValidationIssues;

            if (HasValidationIssues)
            {
                _logger.LogWarning(
                    "Order validation failed: StockIssues={HasStockIssues}, PriceChanges={HasPriceChanges}",
                    HasStockIssues,
                    HasPriceChanges);
            }

            await LoadPageDataAsync(addressId.Value, shippingMethods, cancellationToken);
            return Page();
        }

        // Clear checkout session data
        HttpContext.Session.Remove(Constants.CheckoutAddressIdKey);
        HttpContext.Session.Remove(Constants.CheckoutShippingMethodsKey);
        HttpContext.Session.Remove(Constants.CheckoutPaymentMethodIdKey);

        _logger.LogInformation("Order placed successfully: {OrderNumber}", result.OrderNumber);

        // If payment requires redirect (e.g., 3D Secure, PayPal), redirect to payment provider
        if (result.RequiresRedirect && !string.IsNullOrEmpty(result.PaymentRedirectUrl))
        {
            return Redirect(result.PaymentRedirectUrl);
        }

        // Redirect to confirmation page
        return RedirectToPage("/Buyer/OrderConfirmation", new { orderId = result.OrderId });
    }

    private async Task LoadPageDataAsync(
        Guid addressId,
        Dictionary<Guid, Guid> shippingMethods,
        CancellationToken cancellationToken)
    {
        var (buyerId, sessionId) = GetIdentifiers();

        PaymentData = await _checkoutService.HandleAsync(
            new GetCheckoutPaymentMethodsQuery(buyerId, sessionId, addressId, shippingMethods),
            cancellationToken);

        // Load previously selected payment method
        var storedPaymentMethod = HttpContext.Session.GetString(Constants.CheckoutPaymentMethodIdKey);
        if (!string.IsNullOrEmpty(storedPaymentMethod) && Guid.TryParse(storedPaymentMethod, out var paymentMethodId))
        {
            SelectedPaymentMethodId = paymentMethodId;
        }
        else if (PaymentData?.SelectedPaymentMethodId.HasValue == true)
        {
            SelectedPaymentMethodId = PaymentData.SelectedPaymentMethodId;
        }
    }

    private async Task LoadPageDataForErrorAsync(CancellationToken cancellationToken)
    {
        var addressId = GetSelectedAddressId();
        var shippingMethods = GetSelectedShippingMethods();

        if (addressId.HasValue && shippingMethods.Count > 0)
        {
            await LoadPageDataAsync(addressId.Value, shippingMethods, cancellationToken);
        }
    }

    private Guid? GetSelectedAddressId()
    {
        var storedAddressId = HttpContext.Session.GetString(Constants.CheckoutAddressIdKey);
        if (!string.IsNullOrEmpty(storedAddressId) && Guid.TryParse(storedAddressId, out var addressId))
        {
            return addressId;
        }
        return null;
    }

    private Dictionary<Guid, Guid> GetSelectedShippingMethods()
    {
        var storedMethods = HttpContext.Session.GetString(Constants.CheckoutShippingMethodsKey);
        if (!string.IsNullOrEmpty(storedMethods))
        {
            try
            {
                return JsonSerializer.Deserialize<Dictionary<Guid, Guid>>(storedMethods) ?? new();
            }
            catch (JsonException)
            {
                // Session data was corrupted; reset to empty
                _logger.LogDebug("Failed to deserialize shipping methods from session, resetting to empty");
                return new();
            }
        }
        return new();
    }

    private Guid? GetBuyerId()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var buyerId))
            {
                return buyerId;
            }
        }
        return null;
    }

    private (Guid? BuyerId, string? SessionId) GetIdentifiers()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var buyerId))
            {
                return (buyerId, null);
            }
        }

        var sessionId = HttpContext.Session.GetString(Constants.CartSessionKey);
        if (string.IsNullOrEmpty(sessionId))
        {
            sessionId = Guid.NewGuid().ToString();
            HttpContext.Session.SetString(Constants.CartSessionKey, sessionId);
        }

        return (null, sessionId);
    }
}
