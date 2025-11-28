using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.Services;

namespace SD.Project.Pages
{
    public class ResendVerificationModel : PageModel
    {
        private readonly ILogger<ResendVerificationModel> _logger;
        private readonly EmailVerificationService _emailVerificationService;

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        public string? Message { get; private set; }
        public bool Success { get; private set; }
        public bool ShowForm { get; private set; } = true;

        public ResendVerificationModel(
            ILogger<ResendVerificationModel> logger,
            EmailVerificationService emailVerificationService)
        {
            _logger = logger;
            _emailVerificationService = emailVerificationService;
        }

        public void OnGet(string? email = null)
        {
            if (!string.IsNullOrWhiteSpace(email))
            {
                Email = email;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                Message = "Please enter your email address.";
                return Page();
            }

            var command = new ResendVerificationEmailCommand(Email);
            var result = await _emailVerificationService.HandleAsync(command);

            Success = result.Success;
            Message = result.Message;

            if (result.Success)
            {
                _logger.LogInformation("Verification email resend requested for {Email}", Email);
                ShowForm = false;
            }

            return Page();
        }
    }
}
