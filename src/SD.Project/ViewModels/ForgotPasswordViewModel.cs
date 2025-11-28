using System.ComponentModel.DataAnnotations;

namespace SD.Project.ViewModels;

/// <summary>
/// View model for the forgot password form.
/// </summary>
public sealed class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;
}
