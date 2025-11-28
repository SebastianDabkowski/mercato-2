using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Domain.ValueObjects;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service orchestrating email verification use cases.
/// </summary>
public sealed class EmailVerificationService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationTokenRepository _tokenRepository;
    private readonly INotificationService _notificationService;

    public EmailVerificationService(
        IUserRepository userRepository,
        IEmailVerificationTokenRepository tokenRepository,
        INotificationService notificationService)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Creates a verification token for a user and sends the verification email.
    /// </summary>
    public async Task<string> CreateVerificationTokenAndSendEmailAsync(
        Guid userId,
        string email,
        CancellationToken cancellationToken = default)
    {
        var token = new EmailVerificationToken(userId);
        await _tokenRepository.AddAsync(token, cancellationToken);
        await _tokenRepository.SaveChangesAsync(cancellationToken);

        await _notificationService.SendEmailVerificationAsync(userId, email, token.Token, cancellationToken);

        return token.Token;
    }

    /// <summary>
    /// Handles a request to verify an email using a verification token.
    /// </summary>
    public async Task<EmailVerificationResultDto> HandleAsync(VerifyEmailCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Token))
        {
            return EmailVerificationResultDto.Failed("Verification token is required.");
        }

        var token = await _tokenRepository.GetByTokenAsync(command.Token, cancellationToken);
        if (token is null)
        {
            return EmailVerificationResultDto.Failed("Invalid verification token.");
        }

        if (token.IsUsed)
        {
            return EmailVerificationResultDto.AlreadyUsed("This verification link has already been used.");
        }

        if (token.IsExpired)
        {
            return EmailVerificationResultDto.Expired("This verification link has expired. Please request a new one.");
        }

        var user = await _userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user is null)
        {
            return EmailVerificationResultDto.Failed("User not found.");
        }

        if (user.Status == UserStatus.Verified)
        {
            return EmailVerificationResultDto.Succeeded("Your email is already verified.");
        }

        // Mark token as used and verify user email
        token.MarkAsUsed();
        user.VerifyEmail();

        await _tokenRepository.SaveChangesAsync(cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        var requiresKyc = user.RequiresKyc;
        var message = requiresKyc
            ? "Email verified successfully! Please complete KYC verification to access the seller panel."
            : "Email verified successfully! You can now access your account.";

        return EmailVerificationResultDto.Succeeded(message, requiresKyc);
    }

    /// <summary>
    /// Handles a request to resend the verification email.
    /// </summary>
    public async Task<ResendVerificationResultDto> HandleAsync(ResendVerificationEmailCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return ResendVerificationResultDto.Failed("Email address is required.");
        }

        Email email;
        try
        {
            email = Email.Create(command.Email);
        }
        catch (ArgumentException)
        {
            return ResendVerificationResultDto.Failed("Invalid email address format.");
        }

        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            // For security, don't reveal that the email doesn't exist
            return ResendVerificationResultDto.Succeeded("If an account exists with this email, a verification email has been sent.");
        }

        if (user.Status == UserStatus.Verified)
        {
            return ResendVerificationResultDto.Failed("This email is already verified.");
        }

        if (user.Status == UserStatus.Suspended)
        {
            return ResendVerificationResultDto.Failed("This account has been suspended.");
        }

        // Create new token and send email
        await CreateVerificationTokenAndSendEmailAsync(user.Id, user.Email.Value, cancellationToken);

        return ResendVerificationResultDto.Succeeded("A new verification email has been sent. Please check your inbox.");
    }
}
