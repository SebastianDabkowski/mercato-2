using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.Services;
using SD.Project.ViewModels;

namespace SD.Project.Pages
{
    public class LoginModel : PageModel
    {
        private readonly ILogger<LoginModel> _logger;
        private readonly LoginService _loginService;
        private readonly EmailVerificationService _emailVerificationService;
        private readonly SessionService _sessionService;
        private readonly CartService _cartService;
        private readonly IAuthenticationSchemeProvider _schemeProvider;

        // Custom claim type for session token
        public const string SessionTokenClaimType = "session_token";

        [BindProperty]
        public LoginViewModel Input { get; set; } = new();

        public string? ErrorMessage { get; private set; }
        public bool ShowResendVerification { get; private set; }
        public string? ResendEmail { get; private set; }
        public IReadOnlyList<string> ExternalProviders { get; private set; } = [];

        [TempData]
        public string? StatusMessage { get; set; }

        public LoginModel(
            ILogger<LoginModel> logger,
            LoginService loginService,
            EmailVerificationService emailVerificationService,
            SessionService sessionService,
            CartService cartService,
            IAuthenticationSchemeProvider schemeProvider)
        {
            _logger = logger;
            _loginService = loginService;
            _emailVerificationService = emailVerificationService;
            _sessionService = sessionService;
            _cartService = cartService;
            _schemeProvider = schemeProvider;
        }

        public async Task OnGetAsync()
        {
            await LoadExternalProvidersAsync();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            await LoadExternalProvidersAsync();
            returnUrl ??= Url.Content("~/");

            var command = new LoginCommand(Input.Email, Input.Password);
            var result = await _loginService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation("User {UserId} logged in with role {Role}",
                    result.UserId, result.Role);

                // Create a secure session token
                var userAgent = Request.Headers.UserAgent.ToString();
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var session = await _sessionService.CreateSessionAsync(
                    result.UserId!.Value,
                    Input.RememberMe,
                    userAgent,
                    ipAddress);

                // Create claims for the authenticated user
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, result.UserId!.Value.ToString()),
                    new(ClaimTypes.Email, result.Email!),
                    new(ClaimTypes.Name, result.FirstName!),
                    new(ClaimTypes.Role, result.Role!.Value.ToString()),
                    new(SessionTokenClaimType, session.Token)
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

                // Merge guest cart with user's cart if a guest session exists
                var guestSessionId = HttpContext.Session.GetString(Constants.CartSessionKey);
                if (!string.IsNullOrEmpty(guestSessionId))
                {
                    await _cartService.HandleAsync(new MergeCartsCommand(result.UserId!.Value, guestSessionId));
                    HttpContext.Session.Remove(Constants.CartSessionKey);
                    _logger.LogInformation("Merged guest cart for user {UserId}", result.UserId);
                }

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
            await LoadExternalProvidersAsync();

            if (string.IsNullOrWhiteSpace(email))
            {
                ErrorMessage = "Email address is required.";
                return Page();
            }

            var resendCommand = new ResendVerificationEmailCommand(email);
            var resendResult = await _emailVerificationService.HandleAsync(resendCommand);

            if (resendResult.Success)
            {
                _logger.LogInformation("Verification email resend requested for {Email}", email);
            }

            StatusMessage = resendResult.Message;
            return RedirectToPage();
        }

        private async Task LoadExternalProvidersAsync()
        {
            var schemes = await _schemeProvider.GetAllSchemesAsync();
            var externalSchemes = new[] { "Google", "Facebook" };
            
            ExternalProviders = schemes
                .Where(s => externalSchemes.Contains(s.Name))
                .Select(s => s.Name)
                .ToList();
        }
    }
}
