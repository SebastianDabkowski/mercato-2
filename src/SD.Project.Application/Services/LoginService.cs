using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Domain.ValueObjects;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service orchestrating user login use cases.
/// </summary>
public sealed class LoginService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILoginRateLimiter _rateLimiter;

    // Generic error message to prevent user enumeration attacks
    private const string InvalidCredentialsMessage = "Invalid email or password.";

    public LoginService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ILoginRateLimiter rateLimiter)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _rateLimiter = rateLimiter;
    }

    /// <summary>
    /// Handles a login request.
    /// </summary>
    public async Task<LoginResultDto> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Basic validation
        if (string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrWhiteSpace(command.Password))
        {
            return LoginResultDto.Failed(InvalidCredentialsMessage);
        }

        // Rate limiting check using email as identifier
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        if (_rateLimiter.IsRateLimited(normalizedEmail))
        {
            return LoginResultDto.Failed("Too many login attempts. Please try again later.");
        }

        // Try to parse email
        Email email;
        try
        {
            email = Email.Create(command.Email);
        }
        catch (ArgumentException)
        {
            _rateLimiter.RecordFailedAttempt(normalizedEmail);
            return LoginResultDto.Failed(InvalidCredentialsMessage);
        }

        // Find user by email
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            _rateLimiter.RecordFailedAttempt(normalizedEmail);
            return LoginResultDto.Failed(InvalidCredentialsMessage);
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(command.Password, user.PasswordHash))
        {
            _rateLimiter.RecordFailedAttempt(normalizedEmail);
            return LoginResultDto.Failed(InvalidCredentialsMessage);
        }

        // Check account status
        if (user.Status == UserStatus.Suspended)
        {
            _rateLimiter.RecordFailedAttempt(normalizedEmail);
            return LoginResultDto.Failed("Your account has been suspended. Please contact support.");
        }

        // Check email verification - buyers can login without verification, sellers must verify
        if (user.Status == UserStatus.Unverified && user.Role == UserRole.Seller)
        {
            return LoginResultDto.VerificationRequired(user.Email.Value);
        }

        // Successful login
        _rateLimiter.ResetAttempts(normalizedEmail);

        return LoginResultDto.Succeeded(user.Id, user.Role, user.Email.Value, user.FirstName);
    }
}
