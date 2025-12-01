using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.ViewModels;

namespace SD.Project.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly ILogger<RegisterModel> _logger;
        private readonly RegistrationService _registrationService;
        private readonly UserConsentService _userConsentService;
        private readonly IAuthenticationSchemeProvider _schemeProvider;

        [BindProperty]
        public RegisterViewModel Input { get; set; } = new();

        public IReadOnlyCollection<string> Errors { get; private set; } = Array.Empty<string>();
        public string? SuccessMessage { get; private set; }
        public IReadOnlyList<string> ExternalProviders { get; private set; } = [];
        public IReadOnlyCollection<ConsentTypeDto> ConsentTypes { get; private set; } = Array.Empty<ConsentTypeDto>();

        public RegisterModel(
            ILogger<RegisterModel> logger,
            RegistrationService registrationService,
            UserConsentService userConsentService,
            IAuthenticationSchemeProvider schemeProvider)
        {
            _logger = logger;
            _registrationService = registrationService;
            _userConsentService = userConsentService;
            _schemeProvider = schemeProvider;
        }

        public async Task OnGetAsync()
        {
            await LoadExternalProvidersAsync();
            await LoadConsentTypesAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadExternalProvidersAsync();
            await LoadConsentTypesAsync();

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers.UserAgent.ToString();

            // Build consent decisions from form
            var consentDecisions = Input.ConsentDecisions
                .Select(kvp => new RegistrationConsentDecision(kvp.Key, kvp.Value))
                .ToList();

            var command = new RegisterUserCommand(
                Input.Email,
                Input.Password,
                Input.ConfirmPassword,
                Input.Role,
                Input.FirstName,
                Input.LastName,
                Input.AcceptTerms,
                Input.CompanyName,
                Input.TaxId,
                Input.PhoneNumber,
                consentDecisions,
                ipAddress,
                userAgent);

            var result = await _registrationService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation("User {UserId} registered successfully with role {Role}", 
                    result.UserId, Input.Role);
                SuccessMessage = result.Message;
                // Clear the form on success
                Input = new RegisterViewModel();
                return Page();
            }

            Errors = result.Errors;
            return Page();
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

        private async Task LoadConsentTypesAsync()
        {
            ConsentTypes = await _userConsentService.HandleAsync(new GetActiveConsentTypesQuery());
        }
    }
}
