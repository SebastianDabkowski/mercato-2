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
/// Page model for the checkout shipping step.
/// </summary>
public class CheckoutShippingModel : PageModel
{
    private readonly ILogger<CheckoutShippingModel> _logger;
    private readonly CheckoutService _checkoutService;
    private readonly DeliveryAddressService _addressService;

    public CheckoutShippingDto? ShippingData { get; private set; }
    public Dictionary<Guid, Guid> SelectedShippingMethods { get; private set; } = new();
    public string? DeliveryAddressSummary { get; private set; }
    public string? Message { get; private set; }
    public bool IsSuccess { get; private set; }

    public CheckoutShippingModel(
        ILogger<CheckoutShippingModel> logger,
        CheckoutService checkoutService,
        DeliveryAddressService addressService)
    {
        _logger = logger;
        _checkoutService = checkoutService;
        _addressService = addressService;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
    {
        // Verify address is selected
        var addressId = GetSelectedAddressId();
        if (!addressId.HasValue)
        {
            return RedirectToPage("/Buyer/Checkout");
        }

        await LoadPageDataAsync(addressId.Value, cancellationToken);

        if (ShippingData is null)
        {
            return RedirectToPage("/Buyer/Checkout");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSelectShippingAsync(
        Dictionary<Guid, Guid> shippingSelections,
        CancellationToken cancellationToken = default)
    {
        var addressId = GetSelectedAddressId();
        if (!addressId.HasValue)
        {
            return RedirectToPage("/Buyer/Checkout");
        }

        var (buyerId, sessionId) = GetIdentifiers();

        // Validate selections
        var result = await _checkoutService.HandleAsync(
            new SelectShippingMethodsCommand(buyerId, sessionId, shippingSelections),
            cancellationToken);

        if (!result.IsSuccess)
        {
            IsSuccess = false;
            Message = result.ErrorMessage;
            await LoadPageDataAsync(addressId.Value, cancellationToken);
            return Page();
        }

        // Store selected shipping methods in session
        HttpContext.Session.SetString(
            Constants.CheckoutShippingMethodsKey,
            JsonSerializer.Serialize(shippingSelections));

        _logger.LogInformation("Shipping methods selected for checkout: {Count} sellers", shippingSelections.Count);

        // Redirect to payment page
        return RedirectToPage("/Buyer/CheckoutPayment");
    }

    private async Task LoadPageDataAsync(Guid addressId, CancellationToken cancellationToken)
    {
        var (buyerId, sessionId) = GetIdentifiers();

        // Load shipping options
        ShippingData = await _checkoutService.HandleAsync(
            new GetCheckoutShippingQuery(buyerId, sessionId, addressId),
            cancellationToken);

        // Load address summary
        var address = await _addressService.HandleAsync(
            new GetDeliveryAddressByIdQuery(buyerId, sessionId, addressId),
            cancellationToken);

        if (address is not null)
        {
            DeliveryAddressSummary = $"{address.RecipientName}, {address.Street}, {address.City}, {address.Country}";
        }

        // Load previously selected shipping methods from session
        var storedMethods = HttpContext.Session.GetString(Constants.CheckoutShippingMethodsKey);
        if (!string.IsNullOrEmpty(storedMethods))
        {
            try
            {
                SelectedShippingMethods = JsonSerializer.Deserialize<Dictionary<Guid, Guid>>(storedMethods) ?? new();
            }
            catch
            {
                SelectedShippingMethods = new();
            }
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
