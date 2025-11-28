using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.Services;

namespace SD.Project.Pages
{
    public class VerifyEmailModel : PageModel
    {
        private readonly ILogger<VerifyEmailModel> _logger;
        private readonly EmailVerificationService _emailVerificationService;

        public bool Success { get; private set; }
        public string? Message { get; private set; }
        public bool TokenExpired { get; private set; }
        public bool TokenAlreadyUsed { get; private set; }
        public bool RequiresKyc { get; private set; }

        public VerifyEmailModel(
            ILogger<VerifyEmailModel> logger,
            EmailVerificationService emailVerificationService)
        {
            _logger = logger;
            _emailVerificationService = emailVerificationService;
        }

        public async Task<IActionResult> OnGetAsync(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                Message = "Invalid verification link. Please use the link provided in your email.";
                return Page();
            }

            var command = new VerifyEmailCommand(token);
            var result = await _emailVerificationService.HandleAsync(command);

            Success = result.Success;
            Message = result.Message;
            TokenExpired = result.TokenExpired;
            TokenAlreadyUsed = result.TokenAlreadyUsed;
            RequiresKyc = result.RequiresKyc;

            if (result.Success)
            {
                _logger.LogInformation("Email verified successfully via token: {Token}", token);
            }
            else
            {
                _logger.LogWarning("Email verification failed for token {Token}: {Message}", token, result.Message);
            }

            return Page();
        }
    }
}
