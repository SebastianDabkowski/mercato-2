using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.Services;
using SD.Project.ViewModels;

namespace SD.Project.Pages
{
    [Authorize]
    public class ChangePasswordModel : PageModel
    {
        private readonly ILogger<ChangePasswordModel> _logger;
        private readonly PasswordResetService _passwordResetService;

        [BindProperty]
        public ChangePasswordViewModel Input { get; set; } = new();

        public string? ErrorMessage { get; private set; }
        public IReadOnlyCollection<string>? ValidationErrors { get; private set; }
        public bool ShowSuccess { get; private set; }

        public ChangePasswordModel(
            ILogger<ChangePasswordModel> logger,
            PasswordResetService passwordResetService)
        {
            _logger = logger;
            _passwordResetService = passwordResetService;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                ErrorMessage = "Unable to identify user. Please sign in again.";
                return Page();
            }

            if (Input.NewPassword != Input.ConfirmPassword)
            {
                ErrorMessage = "New passwords do not match.";
                return Page();
            }

            var command = new ChangePasswordCommand(userId, Input.CurrentPassword, Input.NewPassword);
            var result = await _passwordResetService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation("Password changed successfully for user {UserId}", userId);
                ShowSuccess = true;
                return Page();
            }

            ErrorMessage = result.Message;
            ValidationErrors = result.ValidationErrors;
            return Page();
        }
    }
}
