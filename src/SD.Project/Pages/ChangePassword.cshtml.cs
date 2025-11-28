using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
        private readonly SessionService _sessionService;

        [BindProperty]
        public ChangePasswordViewModel Input { get; set; } = new();

        public string? ErrorMessage { get; private set; }
        public IReadOnlyCollection<string>? ValidationErrors { get; private set; }
        public bool ShowSuccess { get; private set; }

        public ChangePasswordModel(
            ILogger<ChangePasswordModel> logger,
            PasswordResetService passwordResetService,
            SessionService sessionService)
        {
            _logger = logger;
            _passwordResetService = passwordResetService;
            _sessionService = sessionService;
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

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var command = new ChangePasswordCommand(userId, Input.CurrentPassword, Input.NewPassword);
            var result = await _passwordResetService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation("Password changed successfully for user {UserId}", userId);

                // Revoke all sessions for security when password is changed
                var revokedCount = await _sessionService.RevokeAllUserSessionsAsync(userId);
                _logger.LogInformation("Revoked {Count} sessions for user {UserId} after password change", revokedCount, userId);

                // Sign out the current session - user will need to re-authenticate
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                // Redirect to login with a success message
                TempData["StatusMessage"] = "Your password has been changed successfully. Please sign in with your new password.";
                return RedirectToPage("/Login");
            }

            ErrorMessage = result.Message;
            ValidationErrors = result.ValidationErrors;
            return Page();
        }
    }
}
