using SD.Project.Domain.Entities;

namespace SD.Project.ViewModels;

/// <summary>
/// View model for the registration form.
/// </summary>
public sealed class RegisterViewModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Buyer;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? TaxId { get; set; }
    public string? PhoneNumber { get; set; }
    public bool AcceptTerms { get; set; }
    
    /// <summary>
    /// Consent decisions made during registration.
    /// Key is the ConsentTypeId, value is whether consent was granted.
    /// </summary>
    public Dictionary<Guid, bool> ConsentDecisions { get; set; } = new();
}
