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
    public class OnboardingCompleteModel : PageModel
    {
        private readonly ILogger<OnboardingCompleteModel> _logger;
        private readonly SellerOnboardingService _onboardingService;
        private readonly IConfiguration _configuration;

        public OnboardingStatus Status { get; private set; }
        public string? StoreName { get; private set; }
        public DateTime? SubmittedAt { get; private set; }
        public string SupportEmail { get; private set; } = "support@example.com";

        public OnboardingCompleteModel(
            ILogger<OnboardingCompleteModel> logger,
            SellerOnboardingService onboardingService,
            IConfiguration configuration)
        {
            _logger = logger;
            _onboardingService = onboardingService;
            _configuration = configuration;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToPage("/Login");
            }

            SupportEmail = _configuration["SupportEmail"] ?? "support@example.com";

            var onboarding = await _onboardingService.HandleAsync(new GetSellerOnboardingQuery(userId));
            if (onboarding is null)
            {
                return RedirectToPage("/Error", new { message = "Unable to load onboarding data." });
            }

            // If onboarding is still in progress, redirect to the wizard
            if (onboarding.Status == OnboardingStatus.InProgress)
            {
                return RedirectToPage("/Seller/Onboarding");
            }

            Status = onboarding.Status;
            StoreName = onboarding.StoreName;
            SubmittedAt = onboarding.SubmittedAt;

            _logger.LogInformation("Seller {UserId} viewed onboarding complete page with status {Status}",
                userId, Status);

            return Page();
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }
}
