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
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordValidator _passwordValidator;
    private readonly INotificationService _notificationService;

    public RegistrationService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IPasswordValidator passwordValidator,
        INotificationService notificationService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _passwordValidator = passwordValidator;
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

        // Validate email format
        Email email;
        try
        {
            email = Email.Create(command.Email);
        }
        catch (ArgumentException ex)
        {
            errors.Add(ex.Message);
            email = null!;
        }

        if (errors.Count > 0)
        {
            return RegistrationResultDto.Failed(errors);
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

        // Send verification email
        await _notificationService.SendEmailVerificationAsync(user.Id, user.Email.Value, cancellationToken);

        return RegistrationResultDto.Succeeded(
            user.Id,
            "Registration successful. Please check your email to verify your account.");
    }
}
