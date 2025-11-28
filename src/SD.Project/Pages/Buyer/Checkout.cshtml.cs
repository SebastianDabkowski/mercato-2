using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.ViewModels;
using System.Security.Claims;

namespace SD.Project.Pages.Buyer;

/// <summary>
/// Page model for the checkout address step.
/// </summary>
public class CheckoutModel : PageModel
{
    private readonly ILogger<CheckoutModel> _logger;
    private readonly CartService _cartService;
    private readonly DeliveryAddressService _addressService;

    public IReadOnlyList<DeliveryAddressViewModel> SavedAddresses { get; private set; } = Array.Empty<DeliveryAddressViewModel>();
    public DeliveryAddressViewModel? SelectedAddress { get; private set; }
    public CartSummaryViewModel? CartSummary { get; private set; }
    public bool IsAuthenticated => User.Identity?.IsAuthenticated == true;

    [BindProperty]
    public Guid? SelectedAddressId { get; set; }

    [BindProperty]
    public CheckoutAddressInputModel AddressInput { get; set; } = new();

    public string? Message { get; private set; }
    public bool IsSuccess { get; private set; }

    public CheckoutModel(
        ILogger<CheckoutModel> logger,
        CartService cartService,
        DeliveryAddressService addressService)
    {
        _logger = logger;
        _cartService = cartService;
        _addressService = addressService;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
    {
        await LoadPageDataAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAddressAsync(CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            await LoadPageDataAsync(cancellationToken);
            return Page();
        }

        var (buyerId, sessionId) = GetIdentifiers();

        // Validate shipping availability first
        var validationResult = await _addressService.HandleAsync(
            new ValidateShippingCommand(
                buyerId,
                sessionId,
                AddressInput.Country,
                AddressInput.State,
                AddressInput.PostalCode),
            cancellationToken);

        if (!validationResult.CanShip)
        {
            IsSuccess = false;
            Message = validationResult.Message;

            if (validationResult.RestrictedProductNames.Any())
            {
                Message += " The following items cannot be shipped to your region: " +
                    string.Join(", ", validationResult.RestrictedProductNames);
            }

            await LoadPageDataAsync(cancellationToken);
            return Page();
        }

        // Save the address
        var result = await _addressService.HandleAsync(
            new SaveDeliveryAddressCommand(
                buyerId,
                sessionId,
                AddressInput.SelectedAddressId,
                AddressInput.RecipientName,
                AddressInput.PhoneNumber,
                AddressInput.Label,
                AddressInput.Street,
                AddressInput.Street2,
                AddressInput.City,
                AddressInput.State,
                AddressInput.PostalCode,
                AddressInput.Country,
                AddressInput.SetAsDefault,
                AddressInput.SaveToProfile),
            cancellationToken);

        if (!result.IsSuccess)
        {
            IsSuccess = false;
            Message = result.ErrorMessage;
            await LoadPageDataAsync(cancellationToken);
            return Page();
        }

        // Store the selected address in session for next steps
        SelectedAddressId = result.Address!.Id;
        HttpContext.Session.SetString(Constants.CheckoutAddressIdKey, result.Address.Id.ToString());

        IsSuccess = true;
        Message = "Address saved successfully.";

        _logger.LogInformation("Address saved for checkout: {AddressId}", result.Address.Id);

        await LoadPageDataAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostUseSelectedAddressAsync(CancellationToken cancellationToken = default)
    {
        if (!SelectedAddressId.HasValue)
        {
            IsSuccess = false;
            Message = "Please select an address.";
            await LoadPageDataAsync(cancellationToken);
            return Page();
        }

        var (buyerId, sessionId) = GetIdentifiers();

        // Verify the address exists and belongs to the user
        var address = await _addressService.HandleAsync(
            new GetDeliveryAddressByIdQuery(buyerId, sessionId, SelectedAddressId.Value),
            cancellationToken);

        if (address is null)
        {
            IsSuccess = false;
            Message = "Selected address not found.";
            await LoadPageDataAsync(cancellationToken);
            return Page();
        }

        // Validate shipping availability
        var validationResult = await _addressService.HandleAsync(
            new ValidateShippingCommand(
                buyerId,
                sessionId,
                address.Country,
                address.State,
                address.PostalCode),
            cancellationToken);

        if (!validationResult.CanShip)
        {
            IsSuccess = false;
            Message = validationResult.Message;
            await LoadPageDataAsync(cancellationToken);
            return Page();
        }

        // Store the selected address in session for next steps
        HttpContext.Session.SetString(Constants.CheckoutAddressIdKey, SelectedAddressId.Value.ToString());

        IsSuccess = true;
        Message = "Address selected. You can proceed to payment.";

        _logger.LogInformation("Address selected for checkout: {AddressId}", SelectedAddressId.Value);

        await LoadPageDataAsync(cancellationToken);
        return Page();
    }

    private async Task LoadPageDataAsync(CancellationToken cancellationToken)
    {
        var (buyerId, sessionId) = GetIdentifiers();

        // Load cart summary
        var cart = await _cartService.HandleAsync(
            new GetCartQuery(buyerId, sessionId),
            cancellationToken);

        if (cart is not null)
        {
            var currency = cart.Currency ?? "USD";
            CartSummary = new CartSummaryViewModel(
                cart.TotalItemCount,
                cart.ItemSubtotal,
                cart.TotalShipping,
                cart.TotalAmount,
                currency);
        }

        // Load saved addresses for authenticated users
        if (buyerId.HasValue)
        {
            var addresses = await _addressService.HandleAsync(
                new GetDeliveryAddressesQuery(buyerId, sessionId),
                cancellationToken);

            SavedAddresses = addresses.Select(MapToViewModel).ToList().AsReadOnly();

            // If no address is selected, try to get from session or use default
            if (!SelectedAddressId.HasValue)
            {
                var storedAddressId = HttpContext.Session.GetString(Constants.CheckoutAddressIdKey);
                if (!string.IsNullOrEmpty(storedAddressId) && Guid.TryParse(storedAddressId, out var storedGuid))
                {
                    SelectedAddressId = storedGuid;
                }
                else
                {
                    // Use default address if available
                    var defaultAddress = addresses.FirstOrDefault(a => a.IsDefault);
                    if (defaultAddress is not null)
                    {
                        SelectedAddressId = defaultAddress.Id;
                    }
                }
            }
        }

        // Load selected address details
        if (SelectedAddressId.HasValue)
        {
            var selectedDto = await _addressService.HandleAsync(
                new GetDeliveryAddressByIdQuery(buyerId, sessionId, SelectedAddressId.Value),
                cancellationToken);

            if (selectedDto is not null)
            {
                SelectedAddress = MapToViewModel(selectedDto);
            }
        }
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

    private static DeliveryAddressViewModel MapToViewModel(DeliveryAddressDto dto)
    {
        var addressParts = new List<string> { dto.Street };
        if (!string.IsNullOrWhiteSpace(dto.Street2))
        {
            addressParts.Add(dto.Street2);
        }
        addressParts.Add(dto.City);
        if (!string.IsNullOrWhiteSpace(dto.State))
        {
            addressParts.Add(dto.State);
        }
        addressParts.Add(dto.PostalCode);
        addressParts.Add(dto.Country);

        var formattedAddress = string.Join(", ", addressParts);

        return new DeliveryAddressViewModel(
            dto.Id,
            dto.RecipientName,
            dto.PhoneNumber,
            dto.Label,
            dto.Street,
            dto.Street2,
            dto.City,
            dto.State,
            dto.PostalCode,
            dto.Country,
            dto.IsDefault,
            formattedAddress);
    }
}
