using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Domain.ValueObjects;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service orchestrating user registration use cases.
/// </summary>
public sealed class RegistrationService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserConsentRepository _userConsentRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordValidator _passwordValidator;
    private readonly EmailVerificationService _emailVerificationService;
    private readonly INotificationService _notificationService;

    public RegistrationService(
        IUserRepository userRepository,
        IUserConsentRepository userConsentRepository,
        IPasswordHasher passwordHasher,
        IPasswordValidator passwordValidator,
        EmailVerificationService emailVerificationService,
        INotificationService notificationService)
    {
        _userRepository = userRepository;
        _userConsentRepository = userConsentRepository;
        _passwordHasher = passwordHasher;
        _passwordValidator = passwordValidator;
        _emailVerificationService = emailVerificationService;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Handles a request to register a new user account.
    /// </summary>
    public async Task<RegistrationResultDto> HandleAsync(RegisterUserCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new List<string>();

        // Validate required fields
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            errors.Add("Email address is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            errors.Add("Password is required.");
        }

        if (string.IsNullOrWhiteSpace(command.FirstName))
        {
            errors.Add("First name is required.");
        }

        if (string.IsNullOrWhiteSpace(command.LastName))
        {
            errors.Add("Last name is required.");
        }

        if (!command.AcceptTerms)
        {
            errors.Add("You must accept the terms and conditions.");
        }

        if (command.Password != command.ConfirmPassword)
        {
            errors.Add("Passwords do not match.");
        }

        // Validate password policy
        if (!string.IsNullOrWhiteSpace(command.Password))
        {
            var passwordErrors = _passwordValidator.Validate(command.Password);
            errors.AddRange(passwordErrors);
        }

        // Validate email format and attempt to create Email value object
        Email? email = null;
        if (!string.IsNullOrWhiteSpace(command.Email))
        {
            try
            {
                email = Email.Create(command.Email);
            }
            catch (ArgumentException ex)
            {
                errors.Add(ex.Message);
            }
        }

        if (errors.Count > 0)
        {
            return RegistrationResultDto.Failed(errors);
        }

        // At this point, email is guaranteed to be non-null because we checked for empty email above
        // and would have returned early if there were validation errors
        if (email is null)
        {
            return RegistrationResultDto.Failed("Email address is required.");
        }

        // Check for duplicate email
        if (await _userRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            return RegistrationResultDto.Failed("An account with this email address already exists.");
        }

        // Create user
        var passwordHash = _passwordHasher.HashPassword(command.Password);
        var user = new User(
            Guid.NewGuid(),
            email,
            passwordHash,
            command.Role,
            command.FirstName,
            command.LastName,
            command.AcceptTerms,
            command.CompanyName,
            command.TaxId,
            command.PhoneNumber);

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        // Record consent decisions if provided (only create records for granted consents)
        if (command.ConsentDecisions is not null && command.ConsentDecisions.Count > 0)
        {
            foreach (var decision in command.ConsentDecisions.Where(d => d.IsGranted))
            {
                var consentType = await _userConsentRepository.GetConsentTypeByIdAsync(
                    decision.ConsentTypeId, cancellationToken);
                
                if (consentType is null || !consentType.IsActive)
                {
                    continue;
                }

                var currentVersion = await _userConsentRepository.GetCurrentVersionAsync(
                    decision.ConsentTypeId, cancellationToken);
                
                if (currentVersion is null)
                {
                    continue;
                }

                var consent = new UserConsent(
                    user.Id,
                    decision.ConsentTypeId,
                    currentVersion.Id,
                    true, // Only granted consents are recorded
                    "registration",
                    command.IpAddress,
                    command.UserAgent);

                await _userConsentRepository.AddAsync(consent, cancellationToken);

                var auditLog = new UserConsentAuditLog(
                    consent.Id,
                    user.Id,
                    UserConsentAuditAction.Granted,
                    currentVersion.Id,
                    "registration",
                    command.IpAddress,
                    command.UserAgent);

                await _userConsentRepository.AddAuditLogAsync(auditLog, cancellationToken);
            }

            await _userConsentRepository.SaveChangesAsync(cancellationToken);
        }

        // Send registration confirmation email to welcome the buyer
        if (command.Role == UserRole.Buyer)
        {
            await _notificationService.SendRegistrationConfirmationAsync(
                user.Id,
                user.Email.Value,
                user.FirstName,
                cancellationToken);
        }

        // Send verification email with unique token
        await _emailVerificationService.CreateVerificationTokenAndSendEmailAsync(user.Id, user.Email.Value, cancellationToken);

        return RegistrationResultDto.Succeeded(
            user.Id,
            "Registration successful. Please check your email to verify your account.");
    }
}
