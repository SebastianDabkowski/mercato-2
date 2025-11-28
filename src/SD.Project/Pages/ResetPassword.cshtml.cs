using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.Services;
using SD.Project.ViewModels;

namespace SD.Project.Pages
{
    public class ResetPasswordModel : PageModel
    {
        private readonly ILogger<ResetPasswordModel> _logger;
        private readonly PasswordResetService _passwordResetService;

        [BindProperty]
        public ResetPasswordViewModel Input { get; set; } = new();

        public string? ErrorMessage { get; private set; }
        public bool ShowSuccess { get; private set; }
        public bool TokenValid { get; private set; }
        public bool TokenExpired { get; private set; }
        public bool TokenAlreadyUsed { get; private set; }

        public ResetPasswordModel(
            ILogger<ResetPasswordModel> logger,
            PasswordResetService passwordResetService)
        {
            _logger = logger;
            _passwordResetService = passwordResetService;
        }

        public async Task<IActionResult> OnGetAsync(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                ErrorMessage = "Invalid password reset link. Please request a new one.";
                return Page();
            }

            Input.Token = token;
            var validationResult = await _passwordResetService.ValidateTokenAsync(token);
            
            if (validationResult.Success)
            {
                TokenValid = true;
            }
            else
            {
                TokenExpired = validationResult.TokenExpired;
                TokenAlreadyUsed = validationResult.TokenAlreadyUsed;
                ErrorMessage = validationResult.Message;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Input.Token))
            {
                ErrorMessage = "Invalid password reset link. Please request a new one.";
                return Page();
            }

            if (!ModelState.IsValid)
            {
                TokenValid = true;
                return Page();
            }

            var command = new ResetPasswordCommand(Input.Token, Input.NewPassword);
            var result = await _passwordResetService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation("Password reset successfully for token");
                ShowSuccess = true;
                return Page();
            }

            TokenExpired = result.TokenExpired;
            TokenAlreadyUsed = result.TokenAlreadyUsed;
            TokenValid = !result.TokenExpired && !result.TokenAlreadyUsed && !result.TokenInvalid;
            ErrorMessage = result.Message;
            return Page();
        }
    }
}
