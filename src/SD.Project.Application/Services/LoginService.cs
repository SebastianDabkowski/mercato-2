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
    private readonly ILoginEventRepository _loginEventRepository;
    private readonly ISecurityAlertService _securityAlertService;

    // Generic error message to prevent user enumeration attacks
    private const string InvalidCredentialsMessage = "Invalid email or password.";

    public LoginService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ILoginRateLimiter rateLimiter,
        ILoginEventRepository loginEventRepository,
        ISecurityAlertService securityAlertService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _rateLimiter = rateLimiter;
        _loginEventRepository = loginEventRepository;
        _securityAlertService = securityAlertService;
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
            await LogLoginEventAsync(null, normalizedEmail, false, LoginEventType.Password, "Invalid email format", command.IpAddress, command.UserAgent, cancellationToken);
            return LoginResultDto.Failed(InvalidCredentialsMessage);
        }

        // Find user by email
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            _rateLimiter.RecordFailedAttempt(normalizedEmail);
            await LogLoginEventAsync(null, normalizedEmail, false, LoginEventType.Password, "User not found", command.IpAddress, command.UserAgent, cancellationToken);
            return LoginResultDto.Failed(InvalidCredentialsMessage);
        }

        // Check if user has a password set (external login users don't have passwords)
        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            _rateLimiter.RecordFailedAttempt(normalizedEmail);
            await LogLoginEventAsync(user.Id, normalizedEmail, false, LoginEventType.Password, "External login user", command.IpAddress, command.UserAgent, cancellationToken);
            return LoginResultDto.Failed("This account uses social login. Please sign in with Google or Facebook.");
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(command.Password, user.PasswordHash))
        {
            _rateLimiter.RecordFailedAttempt(normalizedEmail);
            await LogLoginEventAsync(user.Id, normalizedEmail, false, LoginEventType.Password, "Invalid password", command.IpAddress, command.UserAgent, cancellationToken);
            return LoginResultDto.Failed(InvalidCredentialsMessage);
        }

        // Check account status - blocked accounts
        if (user.Status == UserStatus.Blocked)
        {
            _rateLimiter.RecordFailedAttempt(normalizedEmail);
            await LogLoginEventAsync(user.Id, normalizedEmail, false, LoginEventType.Password, "Account blocked", command.IpAddress, command.UserAgent, cancellationToken);
            return LoginResultDto.Failed("Your account has been blocked. Please contact support for more information.");
        }

        // Check account status - suspended accounts
        if (user.Status == UserStatus.Suspended)
        {
            _rateLimiter.RecordFailedAttempt(normalizedEmail);
            await LogLoginEventAsync(user.Id, normalizedEmail, false, LoginEventType.Password, "Account suspended", command.IpAddress, command.UserAgent, cancellationToken);
            return LoginResultDto.Failed("Your account has been suspended. Please contact support.");
        }

        // Check email verification - buyers can login without verification, sellers must verify
        if (user.Status == UserStatus.Unverified && user.Role == UserRole.Seller)
        {
            await LogLoginEventAsync(user.Id, normalizedEmail, false, LoginEventType.Password, "Email not verified", command.IpAddress, command.UserAgent, cancellationToken);
            return LoginResultDto.VerificationRequired(user.Email.Value);
        }

        // Successful login
        _rateLimiter.ResetAttempts(normalizedEmail);

        // Log successful login event (which includes security alert analysis)
        await LogLoginEventAsync(user.Id, normalizedEmail, true, LoginEventType.Password, null, command.IpAddress, command.UserAgent, cancellationToken);

        // Check if 2FA is required
        if (user.IsTwoFactorConfigured)
        {
            return LoginResultDto.TwoFactorRequired(user.Id, user.Email.Value);
        }

        return LoginResultDto.Succeeded(user.Id, user.Role, user.Email.Value, user.FirstName);
    }

    private async Task LogLoginEventAsync(
        Guid? userId,
        string email,
        bool isSuccess,
        LoginEventType eventType,
        string? failureReason,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var loginEvent = new LoginEvent(
            userId,
            email,
            isSuccess,
            eventType,
            failureReason,
            ipAddress,
            userAgent);

        await _loginEventRepository.AddAsync(loginEvent, cancellationToken);
        await _loginEventRepository.SaveChangesAsync(cancellationToken);

        // Analyze for security alerts if we have a user ID
        if (userId.HasValue)
        {
            var alertTriggered = await _securityAlertService.AnalyzeLoginAsync(
                userId.Value,
                email,
                isSuccess,
                ipAddress,
                userAgent,
                cancellationToken);

            if (alertTriggered)
            {
                loginEvent.MarkAlertTriggered();
                await _loginEventRepository.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
