namespace SD.Project.ViewModels;

/// <summary>
/// View model for the login form.
/// </summary>
public sealed class LoginViewModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}
