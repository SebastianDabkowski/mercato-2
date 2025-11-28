using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Seller
{
    [RequireRole(UserRole.Seller, UserRole.Admin)]
    public class StoreSettingsModel : PageModel
    {
        private readonly ILogger<StoreSettingsModel> _logger;
        private readonly StoreService _storeService;

        [BindProperty]
        public StoreSettingsViewModel Input { get; set; } = new();

        public IReadOnlyCollection<string> Errors { get; private set; } = Array.Empty<string>();
        public string? SuccessMessage { get; private set; }
        public bool HasStore { get; private set; }
        public Guid? StoreId { get; private set; }

        public StoreSettingsModel(
            ILogger<StoreSettingsModel> logger,
            StoreService storeService)
        {
            _logger = logger;
            _storeService = storeService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToPage("/Login");
            }

            var store = await _storeService.HandleAsync(new GetStoreBySellerIdQuery(userId));
            if (store is not null)
            {
                HasStore = true;
                StoreId = store.Id;
                Input.Name = store.Name;
                Input.Description = store.Description;
                Input.ContactEmail = store.ContactEmail;
                Input.PhoneNumber = store.PhoneNumber;
                Input.WebsiteUrl = store.WebsiteUrl;
                Input.LogoUrl = store.LogoUrl;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToPage("/Login");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var existingStore = await _storeService.HandleAsync(new GetStoreBySellerIdQuery(userId));

            if (existingStore is null)
            {
                // Create new store
                var createCommand = new CreateStoreCommand(
                    userId,
                    Input.Name,
                    Input.Description,
                    Input.ContactEmail,
                    Input.PhoneNumber,
                    Input.WebsiteUrl);

                var createResult = await _storeService.HandleAsync(createCommand);

                if (createResult.Success)
                {
                    _logger.LogInformation("Seller {UserId} created store {StoreName}", userId, Input.Name);
                    SuccessMessage = createResult.Message;
                    HasStore = true;
                    StoreId = createResult.Store?.Id;
                }
                else
                {
                    Errors = createResult.Errors;
                }
            }
            else
            {
                // Update existing store
                var updateCommand = new UpdateStoreCommand(
                    userId,
                    Input.Name,
                    Input.Description,
                    Input.ContactEmail,
                    Input.PhoneNumber,
                    Input.WebsiteUrl);

                var updateResult = await _storeService.HandleAsync(updateCommand);

                if (updateResult.Success)
                {
                    _logger.LogInformation("Seller {UserId} updated store {StoreName}", userId, Input.Name);
                    SuccessMessage = updateResult.Message;
                    HasStore = true;
                    StoreId = updateResult.Store?.Id;
                }
                else
                {
                    Errors = updateResult.Errors;
                }
            }

            return Page();
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }
}
