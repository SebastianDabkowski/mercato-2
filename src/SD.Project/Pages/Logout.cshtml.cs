using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Services;

namespace SD.Project.Pages
{
    public class LogoutModel : PageModel
    {
        private readonly ILogger<LogoutModel> _logger;
        private readonly SessionService _sessionService;

        public LogoutModel(ILogger<LogoutModel> logger, SessionService sessionService)
        {
            _logger = logger;
            _sessionService = sessionService;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Revoke the session token in the database
            var sessionToken = User.FindFirst(LoginModel.SessionTokenClaimType)?.Value;
            if (!string.IsNullOrEmpty(sessionToken))
            {
                await _sessionService.RevokeSessionAsync(sessionToken);
                _logger.LogInformation("Session token revoked for user logout");
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("User logged out");
            return RedirectToPage("/Index");
        }
    }
}
