namespace SD.Project.ViewModels;

/// <summary>
/// View model for the reset password form.
/// </summary>
public sealed class ResetPasswordViewModel
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
