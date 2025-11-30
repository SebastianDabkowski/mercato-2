using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Buyer
{
    [RequireRole(UserRole.Buyer, UserRole.Admin)]
    public class AddressesModel : PageModel
    {
        private readonly ILogger<AddressesModel> _logger;
        private readonly DeliveryAddressService _addressService;

        public IReadOnlyList<DeliveryAddressViewModel> Addresses { get; private set; } = [];

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public AddressesModel(
            ILogger<AddressesModel> logger,
            DeliveryAddressService addressService)
        {
            _logger = logger;
            _addressService = addressService;
        }

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
        {
            var buyerId = GetBuyerId();
            if (buyerId == null)
            {
                return RedirectToPage("/Login");
            }

            _logger.LogInformation("Buyer {BuyerId} viewing addresses", buyerId);

            var addresses = await _addressService.HandleAsync(
                new GetDeliveryAddressesQuery(buyerId, null),
                cancellationToken);

            Addresses = addresses
                .Select(MapToViewModel)
                .ToList()
                .AsReadOnly();

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid addressId, CancellationToken cancellationToken = default)
        {
            var buyerId = GetBuyerId();
            if (buyerId == null)
            {
                return RedirectToPage("/Login");
            }

            _logger.LogInformation("Buyer {BuyerId} deleting address {AddressId}", buyerId, addressId);

            var result = await _addressService.HandleAsync(
                new DeleteDeliveryAddressCommand(buyerId.Value, addressId),
                cancellationToken);

            if (result.IsSuccess)
            {
                SuccessMessage = "Address deleted successfully.";
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Failed to delete address.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSetDefaultAsync(Guid addressId, CancellationToken cancellationToken = default)
        {
            var buyerId = GetBuyerId();
            if (buyerId == null)
            {
                return RedirectToPage("/Login");
            }

            _logger.LogInformation("Buyer {BuyerId} setting default address {AddressId}", buyerId, addressId);

            var result = await _addressService.HandleAsync(
                new SetDefaultAddressCommand(buyerId.Value, addressId),
                cancellationToken);

            if (result.IsSuccess)
            {
                SuccessMessage = "Default address updated.";
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Failed to update default address.";
            }

            return RedirectToPage();
        }

        private Guid? GetBuyerId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var buyerId) ? buyerId : null;
        }

        private static DeliveryAddressViewModel MapToViewModel(Application.DTOs.DeliveryAddressDto dto)
        {
            var parts = new List<string> { dto.Street };
            if (!string.IsNullOrWhiteSpace(dto.Street2))
                parts.Add(dto.Street2);
            parts.Add(dto.City);
            if (!string.IsNullOrWhiteSpace(dto.State))
                parts.Add(dto.State);
            parts.Add(dto.PostalCode);
            parts.Add(dto.Country);

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
                string.Join(", ", parts));
        }
    }
}
