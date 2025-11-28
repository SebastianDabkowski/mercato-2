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
    public class StoreProfileModel : PageModel
    {
        private readonly ILogger<StoreProfileModel> _logger;
        private readonly SellerOnboardingService _onboardingService;

        [BindProperty]
        public StoreProfileViewModel Input { get; set; } = new();

        public IReadOnlyCollection<string> Errors { get; private set; } = Array.Empty<string>();
        public string? SuccessMessage { get; private set; }
        public bool StoreProfileCompleted { get; private set; }
        public bool VerificationCompleted { get; private set; }
        public bool PayoutCompleted { get; private set; }

        public StoreProfileModel(
            ILogger<StoreProfileModel> logger,
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

            LoadFromOnboarding(onboarding);
            return Page();
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

            var command = new SaveStoreProfileCommand(
                userId,
                Input.StoreName,
                Input.StoreDescription,
                Input.StoreAddress,
                Input.StoreCity,
                Input.StorePostalCode,
                Input.StoreCountry,
                completeStep);

            var result = await _onboardingService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation("Seller {UserId} saved store profile (complete: {Complete})",
                    userId, completeStep);

                if (completeStep)
                {
                    return RedirectToPage("/Seller/Onboarding/Verification");
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
            Input.StoreName = onboarding.StoreName ?? string.Empty;
            Input.StoreDescription = onboarding.StoreDescription ?? string.Empty;
            Input.StoreAddress = onboarding.StoreAddress ?? string.Empty;
            Input.StoreCity = onboarding.StoreCity ?? string.Empty;
            Input.StorePostalCode = onboarding.StorePostalCode ?? string.Empty;
            Input.StoreCountry = onboarding.StoreCountry ?? string.Empty;
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
