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
    public class OnboardingModel : PageModel
    {
        private readonly ILogger<OnboardingModel> _logger;
        private readonly SellerOnboardingService _onboardingService;

        public OnboardingStep CurrentStep { get; private set; }
        public OnboardingStatus Status { get; private set; }
        public bool StoreProfileCompleted { get; private set; }
        public bool VerificationCompleted { get; private set; }
        public bool PayoutCompleted { get; private set; }

        public OnboardingModel(
            ILogger<OnboardingModel> logger,
            SellerOnboardingService onboardingService)
        {
            _logger = logger;
            _onboardingService = onboardingService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToPage("/Login");
            }

            var onboarding = await _onboardingService.HandleAsync(new GetSellerOnboardingQuery(userId));
            if (onboarding is null)
            {
                return RedirectToPage("/Error", new { message = "Unable to load onboarding data." });
            }

            CurrentStep = onboarding.CurrentStep;
            Status = onboarding.Status;
            StoreProfileCompleted = onboarding.StoreProfileCompleted;
            VerificationCompleted = onboarding.VerificationCompleted;
            PayoutCompleted = onboarding.PayoutCompleted;

            _logger.LogInformation("Seller {UserId} accessed onboarding wizard at step {CurrentStep}",
                userId, CurrentStep);

            // If onboarding is completed, redirect to dashboard
            if (Status != OnboardingStatus.InProgress)
            {
                return RedirectToPage("/Seller/OnboardingComplete");
            }

            // Redirect to the current step page
            return CurrentStep switch
            {
                OnboardingStep.StoreProfile => RedirectToPage("/Seller/Onboarding/StoreProfile"),
                OnboardingStep.Verification => RedirectToPage("/Seller/Onboarding/Verification"),
                OnboardingStep.Payout => RedirectToPage("/Seller/Onboarding/Payout"),
                _ => RedirectToPage("/Seller/OnboardingComplete")
            };
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }
}
