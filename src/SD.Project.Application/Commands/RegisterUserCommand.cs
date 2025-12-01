using SD.Project.Domain.Entities;

namespace SD.Project.Application.Commands;

/// <summary>
/// Represents a consent decision made during registration.
/// </summary>
public sealed record RegistrationConsentDecision(
    Guid ConsentTypeId,
    bool IsGranted);

/// <summary>
/// Command describing a request to register a new user account.
/// </summary>
public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string ConfirmPassword,
    UserRole Role,
    string FirstName,
    string LastName,
    bool AcceptTerms,
    string? CompanyName = null,
    string? TaxId = null,
    string? PhoneNumber = null,
    IReadOnlyList<RegistrationConsentDecision>? ConsentDecisions = null,
    string? IpAddress = null,
    string? UserAgent = null);
