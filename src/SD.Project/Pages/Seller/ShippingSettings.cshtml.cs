using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Seller
{
    [RequireRole(UserRole.Seller, UserRole.Admin)]
    public class ShippingSettingsModel : PageModel
    {
        private readonly ILogger<ShippingSettingsModel> _logger;
        private readonly ShippingMethodService _shippingMethodService;
        private readonly StoreService _storeService;

        public IReadOnlyList<ShippingMethodViewModel> ShippingMethods { get; private set; } = Array.Empty<ShippingMethodViewModel>();

        [BindProperty]
        public ShippingMethodFormViewModel Input { get; set; } = new();

        [BindProperty]
        public Guid? EditingMethodId { get; set; }

        [BindProperty]
        public Guid? ToggleMethodId { get; set; }

        [BindProperty]
        public bool ToggleActiveStatus { get; set; }

        [BindProperty]
        public Guid? DeleteMethodId { get; set; }

        public IReadOnlyCollection<string> Errors { get; private set; } = Array.Empty<string>();
        public string? SuccessMessage { get; private set; }
        public bool HasStore { get; private set; }
        public Guid? StoreId { get; private set; }
        public bool ShowForm { get; private set; }
        public bool IsEditing => EditingMethodId.HasValue;

        public ShippingSettingsModel(
            ILogger<ShippingSettingsModel> logger,
            ShippingMethodService shippingMethodService,
            StoreService storeService)
        {
            _logger = logger;
            _shippingMethodService = shippingMethodService;
            _storeService = storeService;
        }

        public async Task<IActionResult> OnGetAsync(Guid? edit = null)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToPage("/Login");
            }

            var store = await _storeService.HandleAsync(new GetStoreBySellerIdQuery(userId));
            if (store is null)
            {
                HasStore = false;
                return Page();
            }

            HasStore = true;
            StoreId = store.Id;

            await LoadShippingMethodsAsync(store.Id);

            // If editing, load the method into the form
            if (edit.HasValue)
            {
                var method = await _shippingMethodService.HandleAsync(new GetShippingMethodByIdQuery(edit.Value));
                if (method is not null && method.StoreId == store.Id)
                {
                    EditingMethodId = method.Id;
                    ShowForm = true;
                    LoadFormFromDto(method);
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostShowFormAsync()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToPage("/Login");
            }

            var store = await _storeService.HandleAsync(new GetStoreBySellerIdQuery(userId));
            if (store is null)
            {
                return RedirectToPage("/Seller/StoreSettings");
            }

            HasStore = true;
            StoreId = store.Id;
            ShowForm = true;
            Input = new ShippingMethodFormViewModel();

            await LoadShippingMethodsAsync(store.Id);

            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToPage("/Login");
            }

            var store = await _storeService.HandleAsync(new GetStoreBySellerIdQuery(userId));
            if (store is null)
            {
                return RedirectToPage("/Seller/StoreSettings");
            }

            HasStore = true;
            StoreId = store.Id;

            if (!ModelState.IsValid)
            {
                ShowForm = true;
                await LoadShippingMethodsAsync(store.Id);
                return Page();
            }

            ShippingMethodSettingsResultDto result;

            if (EditingMethodId.HasValue)
            {
                // Update existing method
                var updateCommand = new UpdateShippingMethodCommand(
                    EditingMethodId.Value,
                    store.Id,
                    Input.Name,
                    Input.Description,
                    Input.CarrierName,
                    Input.EstimatedDeliveryDaysMin,
                    Input.EstimatedDeliveryDaysMax,
                    Input.BaseCost,
                    Input.CostPerItem,
                    Input.FreeShippingThreshold,
                    Input.DisplayOrder,
                    Input.AvailableRegions);

                result = await _shippingMethodService.HandleAsync(updateCommand);
                if (result.Success)
                {
                    _logger.LogInformation("Seller {UserId} updated shipping method {MethodId}", userId, EditingMethodId.Value);
                }
            }
            else
            {
                // Create new method
                var createCommand = new CreateShippingMethodCommand(
                    store.Id,
                    Input.Name,
                    Input.Description,
                    Input.CarrierName,
                    Input.EstimatedDeliveryDaysMin,
                    Input.EstimatedDeliveryDaysMax,
                    Input.BaseCost,
                    Input.CostPerItem,
                    Input.FreeShippingThreshold,
                    Input.Currency,
                    Input.DisplayOrder,
                    Input.AvailableRegions,
                    Input.IsDefault);

                result = await _shippingMethodService.HandleAsync(createCommand);
                if (result.Success)
                {
                    _logger.LogInformation("Seller {UserId} created shipping method {MethodName}", userId, Input.Name);
                }
            }

            if (result.Success)
            {
                SuccessMessage = result.Message;
                ShowForm = false;
                EditingMethodId = null;
                Input = new ShippingMethodFormViewModel();
            }
            else
            {
                Errors = result.Errors;
                ShowForm = true;
            }

            await LoadShippingMethodsAsync(store.Id);

            return Page();
        }

        public async Task<IActionResult> OnPostToggleStatusAsync()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToPage("/Login");
            }

            var store = await _storeService.HandleAsync(new GetStoreBySellerIdQuery(userId));
            if (store is null)
            {
                return RedirectToPage("/Seller/StoreSettings");
            }

            HasStore = true;
            StoreId = store.Id;

            if (ToggleMethodId.HasValue)
            {
                var command = new ToggleShippingMethodStatusCommand(
                    ToggleMethodId.Value,
                    store.Id,
                    ToggleActiveStatus);

                var result = await _shippingMethodService.HandleAsync(command);

                if (result.Success)
                {
                    _logger.LogInformation("Seller {UserId} toggled shipping method {MethodId} to {Status}",
                        userId, ToggleMethodId.Value, ToggleActiveStatus ? "active" : "inactive");
                    SuccessMessage = result.Message;
                }
                else
                {
                    Errors = result.Errors;
                }
            }

            await LoadShippingMethodsAsync(store.Id);

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToPage("/Login");
            }

            var store = await _storeService.HandleAsync(new GetStoreBySellerIdQuery(userId));
            if (store is null)
            {
                return RedirectToPage("/Seller/StoreSettings");
            }

            HasStore = true;
            StoreId = store.Id;

            if (DeleteMethodId.HasValue)
            {
                var command = new DeleteShippingMethodCommand(DeleteMethodId.Value, store.Id);
                var result = await _shippingMethodService.HandleAsync(command);

                if (result.Success)
                {
                    _logger.LogInformation("Seller {UserId} deleted shipping method {MethodId}", userId, DeleteMethodId.Value);
                    SuccessMessage = result.Message;
                }
                else
                {
                    Errors = result.Errors;
                }
            }

            await LoadShippingMethodsAsync(store.Id);

            return Page();
        }

        public async Task<IActionResult> OnPostCancelAsync()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToPage("/Login");
            }

            var store = await _storeService.HandleAsync(new GetStoreBySellerIdQuery(userId));
            if (store is null)
            {
                return RedirectToPage("/Seller/StoreSettings");
            }

            HasStore = true;
            StoreId = store.Id;
            ShowForm = false;
            EditingMethodId = null;
            Input = new ShippingMethodFormViewModel();

            await LoadShippingMethodsAsync(store.Id);

            return Page();
        }

        private async Task LoadShippingMethodsAsync(Guid storeId)
        {
            var methods = await _shippingMethodService.HandleAsync(new GetShippingMethodsByStoreIdQuery(storeId));
            ShippingMethods = methods.Select(MapToViewModel).ToList();
        }

        private void LoadFormFromDto(ShippingMethodSettingsDto dto)
        {
            Input.Id = dto.Id;
            Input.Name = dto.Name;
            Input.Description = dto.Description;
            Input.CarrierName = dto.CarrierName;
            Input.EstimatedDeliveryDaysMin = dto.EstimatedDeliveryDaysMin;
            Input.EstimatedDeliveryDaysMax = dto.EstimatedDeliveryDaysMax;
            Input.BaseCost = dto.BaseCost;
            Input.CostPerItem = dto.CostPerItem;
            Input.FreeShippingThreshold = dto.FreeShippingThreshold;
            Input.Currency = dto.Currency;
            Input.DisplayOrder = dto.DisplayOrder;
            Input.AvailableRegions = dto.AvailableRegions;
            Input.IsDefault = dto.IsDefault;
        }

        private static ShippingMethodViewModel MapToViewModel(ShippingMethodSettingsDto dto)
        {
            return new ShippingMethodViewModel
            {
                Id = dto.Id,
                Name = dto.Name,
                Description = dto.Description,
                CarrierName = dto.CarrierName,
                EstimatedDeliveryDaysMin = dto.EstimatedDeliveryDaysMin,
                EstimatedDeliveryDaysMax = dto.EstimatedDeliveryDaysMax,
                BaseCost = dto.BaseCost,
                CostPerItem = dto.CostPerItem,
                FreeShippingThreshold = dto.FreeShippingThreshold,
                Currency = dto.Currency,
                DisplayOrder = dto.DisplayOrder,
                AvailableRegions = dto.AvailableRegions,
                IsDefault = dto.IsDefault,
                IsActive = dto.IsActive,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt
            };
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }
}
