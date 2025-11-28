using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.Services;
using SD.Project.ViewModels;

namespace SD.Project.Pages
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly ILogger<ForgotPasswordModel> _logger;
        private readonly PasswordResetService _passwordResetService;

        [BindProperty]
        public ForgotPasswordViewModel Input { get; set; } = new();

        public string? Message { get; private set; }
        public bool ShowSuccess { get; private set; }

        public ForgotPasswordModel(
            ILogger<ForgotPasswordModel> logger,
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
            var command = new ForgotPasswordCommand(Input.Email);
            var result = await _passwordResetService.HandleAsync(command);

            if (result.Success)
            {
                _logger.LogInformation("Password reset requested for email: {Email}", Input.Email);
                ShowSuccess = true;
            }

            Message = result.Message;
            return Page();
        }
    }
}
