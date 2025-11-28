using Microsoft.Extensions.Logging;
using SD.Project.Application.Interfaces;

namespace SD.Project.Infrastructure.Services;

/// <summary>
/// Logs notification intents until real channel is available.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendProductCreatedAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        // TODO: Replace logging with integration (email, message bus, etc.).
        _logger.LogInformation("Product {ProductId} created", productId);
        return Task.CompletedTask;
    }

    public Task SendEmailVerificationAsync(Guid userId, string email, string verificationToken, CancellationToken cancellationToken = default)
    {
        // TODO: Replace logging with real email sending integration.
        // In a production environment, this would generate a full URL like:
        // https://yourdomain.com/VerifyEmail?token={verificationToken}
        var verificationLink = $"/VerifyEmail?token={verificationToken}";
        _logger.LogInformation(
            "Verification email sent to {Email} for user {UserId}. Verification link: {VerificationLink}",
            email, userId, verificationLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(Guid userId, string email, string resetToken, CancellationToken cancellationToken = default)
    {
        // TODO: Replace logging with real email sending integration.
        // In a production environment, this would generate a full URL like:
        // https://yourdomain.com/ResetPassword?token={resetToken}
        var resetLink = $"/ResetPassword?token={resetToken}";
        _logger.LogInformation(
            "Password reset email sent to {Email} for user {UserId}. Reset link: {ResetLink}",
            email, userId, resetLink);
        return Task.CompletedTask;
    }

    public Task SendInternalUserInvitationAsync(string email, string storeName, string role, string invitationToken, CancellationToken cancellationToken = default)
    {
        // TODO: Replace logging with real email sending integration.
        // In a production environment, this would generate a full URL like:
        // https://yourdomain.com/AcceptInvitation?token={invitationToken}
        var invitationLink = $"/AcceptInvitation?token={invitationToken}";
        _logger.LogInformation(
            "Internal user invitation sent to {Email} for store {StoreName} with role {Role}. Invitation link: {InvitationLink}",
            email, storeName, role, invitationLink);
        return Task.CompletedTask;
    }
}
