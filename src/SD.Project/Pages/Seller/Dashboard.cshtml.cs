using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;

namespace SD.Project.Pages.Seller
{
    [RequireRole(UserRole.Seller, UserRole.Admin)]
    public class DashboardModel : PageModel
    {
        private readonly ILogger<DashboardModel> _logger;
        private readonly SellerOnboardingService _onboardingService;

        public bool OnboardingRequired { get; private set; }
        public OnboardingStatus OnboardingStatus { get; private set; }

        public DashboardModel(
            ILogger<DashboardModel> logger,
            SellerOnboardingService onboardingService)
        {
            _logger = logger;
            _onboardingService = onboardingService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("Seller dashboard accessed by user {UserId}", userIdClaim);

            if (Guid.TryParse(userIdClaim, out var userId))
            {
                var onboarding = await _onboardingService.HandleAsync(new GetSellerOnboardingQuery(userId));
                if (onboarding is not null)
                {
                    OnboardingStatus = onboarding.Status;
                    OnboardingRequired = onboarding.Status == OnboardingStatus.InProgress;
                }
                else
                {
                    OnboardingRequired = true;
                    OnboardingStatus = OnboardingStatus.InProgress;
                }
            }

            return Page();
        }
    }
}
