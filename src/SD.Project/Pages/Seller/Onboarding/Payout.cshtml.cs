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
    public class PayoutModel : PageModel
    {
        private readonly ILogger<PayoutModel> _logger;
        private readonly SellerOnboardingService _onboardingService;

        [BindProperty]
        public PayoutViewModel Input { get; set; } = new();

        public IReadOnlyCollection<string> Errors { get; private set; } = Array.Empty<string>();
        public string? SuccessMessage { get; private set; }
        public bool StoreProfileCompleted { get; private set; }
        public bool VerificationCompleted { get; private set; }
        public bool PayoutCompleted { get; private set; }

        public PayoutModel(
            ILogger<PayoutModel> logger,
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

            // If previous steps not completed, redirect back
            if (!onboarding.StoreProfileCompleted)
            {
                return RedirectToPage("/Seller/Onboarding/StoreProfile");
            }

            if (!onboarding.VerificationCompleted)
            {
                return RedirectToPage("/Seller/Onboarding/Verification");
            }

            LoadFromOnboarding(onboarding);
            return Page();
        }

        public async Task<IActionResult> OnPostBackAsync()
        {
            return RedirectToPage("/Seller/Onboarding/Verification");
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            return await SaveData(false);
        }

        public async Task<IActionResult> OnPostSubmitAsync()
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

            var command = new SavePayoutCommand(
                userId,
                Input.BankAccountHolder,
                Input.BankAccountNumber,
                Input.BankName,
                Input.BankSwiftCode,
                completeStep);

            var result = await _onboardingService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation("Seller {UserId} saved payout data (complete: {Complete})",
                    userId, completeStep);

                if (completeStep)
                {
                    return RedirectToPage("/Seller/OnboardingComplete");
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
            Input.BankAccountHolder = onboarding.BankAccountHolder ?? string.Empty;
            Input.BankAccountNumber = onboarding.BankAccountNumber ?? string.Empty;
            Input.BankName = onboarding.BankName ?? string.Empty;
            Input.BankSwiftCode = onboarding.BankSwiftCode ?? string.Empty;
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
