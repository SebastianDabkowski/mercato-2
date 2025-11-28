using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;

namespace SD.Project.Pages;

public class ExternalLoginModel : PageModel
{
    private readonly ILogger<ExternalLoginModel> _logger;
    private readonly ExternalLoginService _externalLoginService;

    public string? ErrorMessage { get; private set; }

    public ExternalLoginModel(
        ILogger<ExternalLoginModel> logger,
        ExternalLoginService externalLoginService)
    {
        _logger = logger;
        _externalLoginService = externalLoginService;
    }

    public IActionResult OnGet()
    {
        // This page is only for displaying errors
        // Normal flow should go through OnGetCallback
        return RedirectToPage("/Login");
    }

    public IActionResult OnPost(string provider, string? returnUrl = null)
    {
        // Validate provider
        if (string.IsNullOrWhiteSpace(provider))
        {
            return RedirectToPage("/Login");
        }

        var authenticationScheme = provider switch
        {
            "Google" => GoogleDefaults.AuthenticationScheme,
            "Facebook" => FacebookDefaults.AuthenticationScheme,
            _ => null
        };

        if (authenticationScheme is null)
        {
            _logger.LogWarning("Invalid external login provider requested: {Provider}", provider);
            return RedirectToPage("/Login");
        }

        // Build the redirect URL for after authentication
        var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUrl,
            Items = { { "LoginProvider", provider } }
        };

        return Challenge(properties, authenticationScheme);
    }

    public async Task<IActionResult> OnGetCallbackAsync(string? returnUrl = null, string? remoteError = null)
    {
        returnUrl ??= Url.Content("~/");

        if (!string.IsNullOrEmpty(remoteError))
        {
            _logger.LogWarning("External login error from provider: {Error}", remoteError);
            ErrorMessage = $"Error from external provider: {remoteError}";
            return Page();
        }

        // The external authentication result comes via the default authentication scheme
        // which is already configured to be cookies in Program.cs
        var result = await HttpContext.AuthenticateAsync();
        if (!result.Succeeded || result.Principal is null)
        {
            _logger.LogWarning("External authentication failed - no principal found");
            ErrorMessage = "External login failed. Please try again.";
            return Page();
        }

        // Extract claims from the external provider
        var claims = result.Principal.Claims.ToList();
        var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var externalId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var firstName = claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value 
            ?? claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value?.Split(' ').FirstOrDefault()
            ?? "User";
        var lastName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value
            ?? claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value?.Split(' ').Skip(1).FirstOrDefault()
            ?? "User";

        // Determine the provider from the authentication result
        string? providerValue = null;
        result.Properties?.Items.TryGetValue("LoginProvider", out providerValue);
        var provider = providerValue ?? "Unknown";
        var externalProvider = provider switch
        {
            "Google" => ExternalLoginProvider.Google,
            "Facebook" => ExternalLoginProvider.Facebook,
            _ => ExternalLoginProvider.None
        };

        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("External login failed - no email provided by {Provider}", provider);
            ErrorMessage = "Email address is required for login. Please ensure you have granted email permission.";
            return Page();
        }

        if (string.IsNullOrEmpty(externalId))
        {
            _logger.LogWarning("External login failed - no external ID provided by {Provider}", provider);
            ErrorMessage = "External login failed. Please try again.";
            return Page();
        }

        if (externalProvider == ExternalLoginProvider.None)
        {
            _logger.LogWarning("External login failed - unknown provider: {Provider}", provider);
            ErrorMessage = "Invalid login provider.";
            return Page();
        }

        // Process the external login
        var command = new ExternalLoginCommand(
            email,
            externalProvider,
            externalId,
            firstName,
            lastName);

        var loginResult = await _externalLoginService.HandleAsync(command);

        if (!loginResult.Success)
        {
            _logger.LogWarning("External login failed for {Email}: {Error}", email, loginResult.ErrorMessage);
            ErrorMessage = loginResult.ErrorMessage;
            return Page();
        }

        _logger.LogInformation("User {UserId} logged in via {Provider} (new user: {IsNew})",
            loginResult.UserId, provider, loginResult.IsNewUser);

        // Create claims for the authenticated user
        var userClaims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, loginResult.UserId!.Value.ToString()),
            new(ClaimTypes.Email, loginResult.Email!),
            new(ClaimTypes.Name, loginResult.FirstName!),
            new(ClaimTypes.Role, loginResult.Role!.Value.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(
            userClaims, CookieAuthenticationDefaults.AuthenticationScheme);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        return LocalRedirect(returnUrl);
    }
}
