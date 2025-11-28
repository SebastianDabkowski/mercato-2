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
    public class PayoutSettingsModel : PageModel
    {
        private readonly ILogger<PayoutSettingsModel> _logger;
        private readonly PayoutSettingsService _payoutSettingsService;
        private readonly SellerOnboardingService _onboardingService;

        [BindProperty]
        public PayoutSettingsPageViewModel Input { get; set; } = new();

        public IReadOnlyCollection<string> Errors { get; private set; } = Array.Empty<string>();
        public string? SuccessMessage { get; private set; }
        public bool IsConfigured { get; private set; }
        public bool IsVerified { get; private set; }
        public bool IsBankTransferAvailable { get; private set; }
        public bool IsSepaAvailable { get; private set; }
        public PayoutMethod DefaultPayoutMethod { get; private set; }
        public bool OnboardingCompleted { get; private set; }

        public PayoutSettingsModel(
            ILogger<PayoutSettingsModel> logger,
            PayoutSettingsService payoutSettingsService,
            SellerOnboardingService onboardingService)
        {
            _logger = logger;
            _payoutSettingsService = payoutSettingsService;
            _onboardingService = onboardingService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToPage("/Login");
            }

            // Check if onboarding is completed
            var onboarding = await _onboardingService.HandleAsync(new GetSellerOnboardingQuery(userId));
            OnboardingCompleted = onboarding?.Status == OnboardingStatus.Verified || 
                                  onboarding?.Status == OnboardingStatus.PendingVerification;

            var settings = await _payoutSettingsService.HandleAsync(new GetPayoutSettingsQuery(userId));
            if (settings is not null)
            {
                LoadFromSettings(settings);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSaveBankTransferAsync()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToPage("/Login");
            }

            var command = new SaveBankTransferPayoutCommand(
                userId,
                Input.BankAccountHolder,
                Input.BankAccountNumber,
                Input.BankName,
                Input.BankSwiftCode,
                Input.BankCountry,
                SetAsDefault: false);

            var result = await _payoutSettingsService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation("Seller {UserId} saved bank transfer configuration", userId);
                SuccessMessage = result.Message;
                if (result.Settings is not null)
                {
                    LoadFromSettings(result.Settings);
                }
            }
            else
            {
                Errors = result.Errors;
            }

            await LoadOnboardingStatus(userId);
            return Page();
        }

        public async Task<IActionResult> OnPostSetBankTransferDefaultAsync()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToPage("/Login");
            }

            // First save the bank transfer details
            var saveCommand = new SaveBankTransferPayoutCommand(
                userId,
                Input.BankAccountHolder,
                Input.BankAccountNumber,
                Input.BankName,
                Input.BankSwiftCode,
                Input.BankCountry,
                SetAsDefault: true);

            var result = await _payoutSettingsService.HandleAsync(saveCommand);

            if (result.Success)
            {
                _logger.LogInformation("Seller {UserId} set bank transfer as default payout method", userId);
                SuccessMessage = result.Message;
                if (result.Settings is not null)
                {
                    LoadFromSettings(result.Settings);
                }
            }
            else
            {
                Errors = result.Errors;
            }

            await LoadOnboardingStatus(userId);
            return Page();
        }

        public async Task<IActionResult> OnPostSaveSepaAsync()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToPage("/Login");
            }

            var command = new SaveSepaPayoutCommand(
                userId,
                Input.SepaIban,
                Input.SepaBic,
                SetAsDefault: false);

            var result = await _payoutSettingsService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation("Seller {UserId} saved SEPA configuration", userId);
                SuccessMessage = result.Message;
                if (result.Settings is not null)
                {
                    LoadFromSettings(result.Settings);
                }
            }
            else
            {
                Errors = result.Errors;
            }

            await LoadOnboardingStatus(userId);
            return Page();
        }

        public async Task<IActionResult> OnPostSetSepaDefaultAsync()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return RedirectToPage("/Login");
            }

            // First save the SEPA details
            var saveCommand = new SaveSepaPayoutCommand(
                userId,
                Input.SepaIban,
                Input.SepaBic,
                SetAsDefault: true);

            var result = await _payoutSettingsService.HandleAsync(saveCommand);

            if (result.Success)
            {
                _logger.LogInformation("Seller {UserId} set SEPA as default payout method", userId);
                SuccessMessage = result.Message;
                if (result.Settings is not null)
                {
                    LoadFromSettings(result.Settings);
                }
            }
            else
            {
                Errors = result.Errors;
            }

            await LoadOnboardingStatus(userId);
            return Page();
        }

        private void LoadFromSettings(Application.DTOs.PayoutSettingsDto settings)
        {
            Input.BankAccountHolder = settings.BankAccountHolder ?? string.Empty;
            Input.BankAccountNumber = settings.BankAccountNumber ?? string.Empty;
            Input.BankName = settings.BankName ?? string.Empty;
            Input.BankSwiftCode = settings.BankSwiftCode ?? string.Empty;
            Input.BankCountry = settings.BankCountry;
            Input.SepaIban = settings.SepaIban ?? string.Empty;
            Input.SepaBic = settings.SepaBic ?? string.Empty;
            IsConfigured = settings.IsConfigured;
            IsVerified = settings.IsVerified;
            IsBankTransferAvailable = settings.IsBankTransferAvailable;
            IsSepaAvailable = settings.IsSepaAvailable;
            DefaultPayoutMethod = settings.DefaultPayoutMethod;
        }

        private async Task LoadOnboardingStatus(Guid userId)
        {
            var onboarding = await _onboardingService.HandleAsync(new GetSellerOnboardingQuery(userId));
            OnboardingCompleted = onboarding?.Status == OnboardingStatus.Verified ||
                                  onboarding?.Status == OnboardingStatus.PendingVerification;
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }
}
