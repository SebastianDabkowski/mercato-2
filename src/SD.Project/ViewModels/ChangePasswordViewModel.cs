namespace SD.Project.ViewModels;

/// <summary>
/// View model for the change password form.
/// </summary>
public sealed class ChangePasswordViewModel
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
