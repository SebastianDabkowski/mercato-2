using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.ViewModels;

namespace SD.Project.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly ILogger<RegisterModel> _logger;
        private readonly RegistrationService _registrationService;

        [BindProperty]
        public RegisterViewModel Input { get; set; } = new();

        public IReadOnlyCollection<string> Errors { get; private set; } = Array.Empty<string>();
        public string? SuccessMessage { get; private set; }

        public RegisterModel(ILogger<RegisterModel> logger, RegistrationService registrationService)
        {
            _logger = logger;
            _registrationService = registrationService;
        }

        public void OnGet()
        {
            // Display the registration form
        }

        public async Task<IActionResult> OnPostAsync()
        {
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
                Input.PhoneNumber);

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
    }
}
