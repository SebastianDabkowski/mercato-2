using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Services;
using SD.Project.ViewModels;

namespace SD.Project.Pages
{
    public class LoginModel : PageModel
    {
        private readonly ILogger<LoginModel> _logger;
        private readonly LoginService _loginService;
        private readonly INotificationService _notificationService;

        [BindProperty]
        public LoginViewModel Input { get; set; } = new();

        public string? ErrorMessage { get; private set; }
        public bool ShowResendVerification { get; private set; }
        public string? ResendEmail { get; private set; }

        [TempData]
        public string? StatusMessage { get; set; }

        public LoginModel(
            ILogger<LoginModel> logger,
            LoginService loginService,
            INotificationService notificationService)
        {
            _logger = logger;
            _loginService = loginService;
            _notificationService = notificationService;
        }

        public void OnGet()
        {
            // Display the login form
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            var command = new LoginCommand(Input.Email, Input.Password);
            var result = await _loginService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation("User {UserId} logged in with role {Role}",
                    result.UserId, result.Role);

                // Create claims for the authenticated user
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, result.UserId!.Value.ToString()),
                    new(ClaimTypes.Email, result.Email!),
                    new(ClaimTypes.Name, result.FirstName!),
                    new(ClaimTypes.Role, result.Role!.Value.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = Input.RememberMe,
                    ExpiresUtc = Input.RememberMe
                        ? DateTimeOffset.UtcNow.AddDays(30)
                        : DateTimeOffset.UtcNow.AddHours(24)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return LocalRedirect(returnUrl);
            }

            if (result.RequiresEmailVerification)
            {
                ShowResendVerification = true;
                ResendEmail = result.Email;
            }

            ErrorMessage = result.ErrorMessage;
            return Page();
        }

        public async Task<IActionResult> OnPostResendVerificationAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ErrorMessage = "Email address is required.";
                return Page();
            }

            // Note: We send a generic success message even if the email doesn't exist
            // to prevent user enumeration attacks
            await _notificationService.SendEmailVerificationAsync(Guid.Empty, email);

            StatusMessage = "If an account exists with this email address, a verification email has been sent.";
            return RedirectToPage();
        }
    }
}
