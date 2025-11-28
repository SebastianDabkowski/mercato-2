using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Services;
using SD.Project.Domain.Repositories;
using SD.Project.Domain.ValueObjects;
using SD.Project.ViewModels;

namespace SD.Project.Pages
{
    public class LoginModel : PageModel
    {
        private readonly ILogger<LoginModel> _logger;
        private readonly LoginService _loginService;
        private readonly INotificationService _notificationService;
        private readonly IUserRepository _userRepository;

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
            INotificationService notificationService,
            IUserRepository userRepository)
        {
            _logger = logger;
            _loginService = loginService;
            _notificationService = notificationService;
            _userRepository = userRepository;
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

            // Look up the user by email to get the actual user ID
            // We use a generic success message regardless of whether the user exists
            // to prevent user enumeration attacks
            try
            {
                var emailObj = Email.Create(email);
                var user = await _userRepository.GetByEmailAsync(emailObj);
                if (user != null)
                {
                    await _notificationService.SendEmailVerificationAsync(user.Id, email);
                    _logger.LogInformation("Verification email resent to user {UserId}", user.Id);
                }
            }
            catch (ArgumentException)
            {
                // Invalid email format - silently ignore to prevent enumeration
            }

            StatusMessage = "If an account exists with this email address, a verification email has been sent.";
            return RedirectToPage();
        }
    }
}
