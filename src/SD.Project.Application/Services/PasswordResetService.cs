using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Domain.ValueObjects;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service orchestrating password reset and change use cases.
/// </summary>
public sealed class PasswordResetService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _tokenRepository;
    private readonly INotificationService _notificationService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordValidator _passwordValidator;

    public PasswordResetService(
        IUserRepository userRepository,
        IPasswordResetTokenRepository tokenRepository,
        INotificationService notificationService,
        IPasswordHasher passwordHasher,
        IPasswordValidator passwordValidator)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _notificationService = notificationService;
        _passwordHasher = passwordHasher;
        _passwordValidator = passwordValidator;
    }

    /// <summary>
    /// Handles a forgot password request. Always returns success to prevent email enumeration.
    /// </summary>
    public async Task<ForgotPasswordResultDto> HandleAsync(ForgotPasswordCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return ForgotPasswordResultDto.Failed("Email address is required.");
        }

        Email email;
        try
        {
            email = Email.Create(command.Email);
        }
        catch (ArgumentException)
        {
            // Return success message to prevent email enumeration even for invalid format
            return ForgotPasswordResultDto.Succeeded();
        }

        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        
        // Always return success to prevent revealing whether email exists
        if (user is null)
        {
            return ForgotPasswordResultDto.Succeeded();
        }

        // Don't send reset email for external login users (they don't have passwords)
        if (user.ExternalProvider != ExternalLoginProvider.None)
        {
            return ForgotPasswordResultDto.Succeeded();
        }

        // Don't send reset email for suspended users
        if (user.Status == UserStatus.Suspended)
        {
            return ForgotPasswordResultDto.Succeeded();
        }

        // Invalidate existing valid tokens by not using them (they'll expire naturally)
        // Create new token and send email
        var token = new PasswordResetToken(user.Id);
        await _tokenRepository.AddAsync(token, cancellationToken);
        await _tokenRepository.SaveChangesAsync(cancellationToken);

        await _notificationService.SendPasswordResetEmailAsync(user.Id, user.Email.Value, token.Token, cancellationToken);

        return ForgotPasswordResultDto.Succeeded();
    }

    /// <summary>
    /// Validates a password reset token without consuming it.
    /// </summary>
    public async Task<ResetPasswordResultDto> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return ResetPasswordResultDto.Invalid("Invalid password reset link.");
        }

        var resetToken = await _tokenRepository.GetByTokenAsync(token, cancellationToken);
        if (resetToken is null)
        {
            return ResetPasswordResultDto.Invalid("Invalid password reset link.");
        }

        if (resetToken.IsUsed)
        {
            return ResetPasswordResultDto.AlreadyUsed("This password reset link has already been used. Please request a new one.");
        }

        if (resetToken.IsExpired)
        {
            return ResetPasswordResultDto.Expired("This password reset link has expired. Please request a new one.");
        }

        return ResetPasswordResultDto.Succeeded("Token is valid.");
    }

    /// <summary>
    /// Handles a password reset using a valid token.
    /// </summary>
    public async Task<ResetPasswordResultDto> HandleAsync(ResetPasswordCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Token))
        {
            return ResetPasswordResultDto.Invalid("Invalid password reset link.");
        }

        if (string.IsNullOrWhiteSpace(command.NewPassword))
        {
            return ResetPasswordResultDto.Failed("New password is required.");
        }

        var resetToken = await _tokenRepository.GetByTokenAsync(command.Token, cancellationToken);
        if (resetToken is null)
        {
            return ResetPasswordResultDto.Invalid("Invalid password reset link.");
        }

        if (resetToken.IsUsed)
        {
            return ResetPasswordResultDto.AlreadyUsed("This password reset link has already been used. Please request a new one.");
        }

        if (resetToken.IsExpired)
        {
            return ResetPasswordResultDto.Expired("This password reset link has expired. Please request a new one.");
        }

        // Validate new password
        var validationErrors = _passwordValidator.Validate(command.NewPassword);
        if (validationErrors.Count > 0)
        {
            return ResetPasswordResultDto.Failed(string.Join(" ", validationErrors));
        }

        var user = await _userRepository.GetByIdAsync(resetToken.UserId, cancellationToken);
        if (user is null)
        {
            return ResetPasswordResultDto.Failed("User not found.");
        }

        if (user.Status == UserStatus.Suspended)
        {
            return ResetPasswordResultDto.Failed("This account has been suspended.");
        }

        // Mark token as used (single-use)
        resetToken.MarkAsUsed();
        await _tokenRepository.SaveChangesAsync(cancellationToken);

        // Invalidate all other valid tokens for this user
        var otherValidTokens = await _tokenRepository.GetValidTokensForUserAsync(user.Id, cancellationToken);
        foreach (var otherToken in otherValidTokens)
        {
            if (otherToken.Id != resetToken.Id)
            {
                otherToken.MarkAsUsed();
            }
        }
        await _tokenRepository.SaveChangesAsync(cancellationToken);

        // Update password
        var newHash = _passwordHasher.HashPassword(command.NewPassword);
        user.SetPassword(newHash);
        await _userRepository.SaveChangesAsync(cancellationToken);

        // Note: Session invalidation would typically be handled by the authentication layer
        // by tracking a "password changed at" timestamp on the user and validating it in the auth cookie

        return ResetPasswordResultDto.Succeeded("Your password has been reset successfully. You can now sign in with your new password.");
    }

    /// <summary>
    /// Handles a password change for an authenticated user.
    /// </summary>
    public async Task<ChangePasswordResultDto> HandleAsync(ChangePasswordCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.UserId == Guid.Empty)
        {
            return ChangePasswordResultDto.Failed("User not authenticated.");
        }

        if (string.IsNullOrWhiteSpace(command.CurrentPassword))
        {
            return ChangePasswordResultDto.Failed("Current password is required.");
        }

        if (string.IsNullOrWhiteSpace(command.NewPassword))
        {
            return ChangePasswordResultDto.Failed("New password is required.");
        }

        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return ChangePasswordResultDto.Failed("User not found.");
        }

        if (user.ExternalProvider != ExternalLoginProvider.None)
        {
            return ChangePasswordResultDto.Failed("Password cannot be changed for accounts using social login.");
        }

        if (user.Status == UserStatus.Suspended)
        {
            return ChangePasswordResultDto.Failed("This account has been suspended.");
        }

        // Verify current password
        if (string.IsNullOrEmpty(user.PasswordHash) || !_passwordHasher.VerifyPassword(command.CurrentPassword, user.PasswordHash))
        {
            return ChangePasswordResultDto.Failed("Current password is incorrect.");
        }

        // Validate new password
        var validationErrors = _passwordValidator.Validate(command.NewPassword);
        if (validationErrors.Count > 0)
        {
            return ChangePasswordResultDto.ValidationFailed(validationErrors);
        }

        // Update password
        var newHash = _passwordHasher.HashPassword(command.NewPassword);
        user.SetPassword(newHash);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return ChangePasswordResultDto.Succeeded("Your password has been changed successfully.");
    }
}
