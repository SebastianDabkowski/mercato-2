using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;

namespace SD.Project.Pages.Buyer
{
    [RequireRole(UserRole.Buyer, UserRole.Admin)]
    public class DashboardModel : PageModel
    {
        private readonly ILogger<DashboardModel> _logger;
        private readonly CheckoutService _checkoutService;

        public IReadOnlyList<OrderSummaryDto> RecentOrders { get; private set; } = [];

        public DashboardModel(
            ILogger<DashboardModel> logger,
            CheckoutService checkoutService)
        {
            _logger = logger;
            _checkoutService = checkoutService;
        }

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("Buyer dashboard accessed by user {UserId}", userIdClaim);

            if (Guid.TryParse(userIdClaim, out var buyerId))
            {
                RecentOrders = await _checkoutService.HandleAsync(
                    new GetBuyerOrdersQuery(buyerId, 0, 10),
                    cancellationToken);
            }

            return Page();
        }
    }
}
