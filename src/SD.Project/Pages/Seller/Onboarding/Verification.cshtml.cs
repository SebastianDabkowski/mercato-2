using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Seller.Onboarding
{
    [RequireRole(UserRole.Seller, UserRole.Admin)]
    public class VerificationModel : PageModel
    {
        private readonly ILogger<VerificationModel> _logger;
        private readonly SellerOnboardingService _onboardingService;

        [BindProperty]
        public VerificationViewModel Input { get; set; } = new();

        public IReadOnlyCollection<string> Errors { get; private set; } = Array.Empty<string>();
        public string? SuccessMessage { get; private set; }
        public bool StoreProfileCompleted { get; private set; }
        public bool VerificationCompleted { get; private set; }
        public bool PayoutCompleted { get; private set; }

        public VerificationModel(
            ILogger<VerificationModel> logger,
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

            if (onboarding.Status != OnboardingStatus.InProgress)
            {
                return RedirectToPage("/Seller/OnboardingComplete");
            }

            // If store profile not completed, redirect back
            if (!onboarding.StoreProfileCompleted)
            {
                return RedirectToPage("/Seller/Onboarding/StoreProfile");
            }

            LoadFromOnboarding(onboarding);
            return Page();
        }

        public async Task<IActionResult> OnPostBackAsync()
        {
            return RedirectToPage("/Seller/Onboarding/StoreProfile");
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            return await SaveData(false);
        }

        public async Task<IActionResult> OnPostNextAsync()
        {
            return await SaveData(true);
        }

        private async Task<IActionResult> SaveData(bool completeStep)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToPage("/Login");
            }

            var command = new SaveVerificationCommand(
                userId,
                Input.BusinessName,
                Input.BusinessRegistrationNumber,
                Input.TaxIdentificationNumber,
                Input.BusinessAddress,
                completeStep);

            var result = await _onboardingService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation("Seller {UserId} saved verification data (complete: {Complete})",
                    userId, completeStep);

                if (completeStep)
                {
                    return RedirectToPage("/Seller/Onboarding/Payout");
                }

                SuccessMessage = result.Message;
            }
            else
            {
                Errors = result.Errors;
            }

            // Reload onboarding state for progress display
            var onboarding = await _onboardingService.HandleAsync(new GetSellerOnboardingQuery(userId));
            if (onboarding is not null)
            {
                StoreProfileCompleted = onboarding.StoreProfileCompleted;
                VerificationCompleted = onboarding.VerificationCompleted;
                PayoutCompleted = onboarding.PayoutCompleted;
            }

            return Page();
        }

        private void LoadFromOnboarding(Application.DTOs.SellerOnboardingDto onboarding)
        {
            Input.BusinessName = onboarding.BusinessName ?? string.Empty;
            Input.BusinessRegistrationNumber = onboarding.BusinessRegistrationNumber ?? string.Empty;
            Input.TaxIdentificationNumber = onboarding.TaxIdentificationNumber ?? string.Empty;
            Input.BusinessAddress = onboarding.BusinessAddress ?? string.Empty;
            StoreProfileCompleted = onboarding.StoreProfileCompleted;
            VerificationCompleted = onboarding.VerificationCompleted;
            PayoutCompleted = onboarding.PayoutCompleted;
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }
}
