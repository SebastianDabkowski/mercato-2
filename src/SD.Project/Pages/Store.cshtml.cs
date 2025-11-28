using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;

namespace SD.Project.Pages
{
    public class StoreModel : PageModel
    {
        private readonly ILogger<StoreModel> _logger;
        private readonly StoreService _storeService;

        public StoreDto? Store { get; private set; }
        public bool StoreNotFound { get; private set; }

        public StoreModel(
            ILogger<StoreModel> logger,
            StoreService storeService)
        {
            _logger = logger;
            _storeService = storeService;
        }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                StoreNotFound = true;
                return Page();
            }

            Store = await _storeService.HandleAsync(new GetStoreByIdQuery(id));

            if (Store is null)
            {
                _logger.LogWarning("Store not found: {StoreId}", id);
                StoreNotFound = true;
            }

            return Page();
        }
    }
}
