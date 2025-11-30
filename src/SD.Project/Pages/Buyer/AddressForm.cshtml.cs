using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace SD.Project.Pages.Buyer
{
    [RequireRole(UserRole.Buyer, UserRole.Admin)]
    public class AddressFormModel : PageModel
    {
        private readonly ILogger<AddressFormModel> _logger;
        private readonly DeliveryAddressService _addressService;

        [BindProperty]
        public AddressInput Input { get; set; } = new();

        public bool IsEditMode => Input.Id.HasValue;

        public string PageTitle => IsEditMode ? "Edit Address" : "Add New Address";

        [TempData]
        public string? ErrorMessage { get; set; }

        public AddressFormModel(
            ILogger<AddressFormModel> logger,
            DeliveryAddressService addressService)
        {
            _logger = logger;
            _addressService = addressService;
        }

        public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken cancellationToken = default)
        {
            var buyerId = GetBuyerId();
            if (buyerId == null)
            {
                return RedirectToPage("/Login");
            }

            if (id.HasValue)
            {
                // Edit mode - load existing address
                var address = await _addressService.HandleAsync(
                    new GetDeliveryAddressByIdQuery(buyerId, null, id.Value),
                    cancellationToken);

                if (address == null)
                {
                    return RedirectToPage("/Buyer/Addresses");
                }

                Input = new AddressInput
                {
                    Id = address.Id,
                    RecipientName = address.RecipientName,
                    PhoneNumber = address.PhoneNumber,
                    Label = address.Label,
                    Street = address.Street,
                    Street2 = address.Street2,
                    City = address.City,
                    State = address.State,
                    PostalCode = address.PostalCode,
                    Country = address.Country,
                    SetAsDefault = address.IsDefault
                };

                _logger.LogInformation("Buyer {BuyerId} editing address {AddressId}", buyerId, id);
            }
            else
            {
                _logger.LogInformation("Buyer {BuyerId} adding new address", buyerId);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken = default)
        {
            var buyerId = GetBuyerId();
            if (buyerId == null)
            {
                return RedirectToPage("/Login");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var command = new SaveDeliveryAddressCommand(
                buyerId,
                null,
                Input.Id,
                Input.RecipientName,
                Input.PhoneNumber,
                Input.Label,
                Input.Street,
                Input.Street2,
                Input.City,
                Input.State,
                Input.PostalCode,
                Input.Country,
                Input.SetAsDefault,
                SaveToProfile: true);  // Always save to profile for authenticated buyers

            var result = await _addressService.HandleAsync(command, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Buyer {BuyerId} {Action} address {AddressId}",
                    buyerId,
                    Input.Id.HasValue ? "updated" : "created",
                    result.Address?.Id);

                TempData["SuccessMessage"] = Input.Id.HasValue
                    ? "Address updated successfully."
                    : "Address added successfully.";

                return RedirectToPage("/Buyer/Addresses");
            }

            ErrorMessage = result.ErrorMessage ?? "An error occurred while saving the address.";
            return Page();
        }

        private Guid? GetBuyerId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var buyerId) ? buyerId : null;
        }

        public sealed class AddressInput
        {
            public Guid? Id { get; set; }

            [Required(ErrorMessage = "Recipient name is required.")]
            [StringLength(200, ErrorMessage = "Recipient name cannot exceed 200 characters.")]
            [Display(Name = "Recipient Name")]
            public string RecipientName { get; set; } = string.Empty;

            [Phone(ErrorMessage = "Please enter a valid phone number.")]
            [Display(Name = "Phone Number")]
            public string? PhoneNumber { get; set; }

            [StringLength(50, ErrorMessage = "Label cannot exceed 50 characters.")]
            [Display(Name = "Label (e.g., Home, Work)")]
            public string? Label { get; set; }

            [Required(ErrorMessage = "Street address is required.")]
            [StringLength(200, ErrorMessage = "Street address cannot exceed 200 characters.")]
            [Display(Name = "Street Address")]
            public string Street { get; set; } = string.Empty;

            [StringLength(200, ErrorMessage = "Address line 2 cannot exceed 200 characters.")]
            [Display(Name = "Address Line 2 (Optional)")]
            public string? Street2 { get; set; }

            [Required(ErrorMessage = "City is required.")]
            [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
            [Display(Name = "City")]
            public string City { get; set; } = string.Empty;

            [StringLength(100, ErrorMessage = "State/Province cannot exceed 100 characters.")]
            [Display(Name = "State/Province")]
            public string? State { get; set; }

            [Required(ErrorMessage = "Postal code is required.")]
            [StringLength(20, ErrorMessage = "Postal code cannot exceed 20 characters.")]
            [Display(Name = "Postal Code")]
            public string PostalCode { get; set; } = string.Empty;

            [Required(ErrorMessage = "Country is required.")]
            [Display(Name = "Country")]
            public string Country { get; set; } = string.Empty;

            [Display(Name = "Set as default shipping address")]
            public bool SetAsDefault { get; set; }
        }
    }
}
