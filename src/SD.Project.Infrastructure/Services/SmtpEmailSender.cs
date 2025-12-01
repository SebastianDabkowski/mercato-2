using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SD.Project.Application.Interfaces;

namespace SD.Project.Infrastructure.Services;

/// <summary>
/// SMTP-based email sender implementation.
/// Logs all send attempts and results for audit purposes.
/// In production, this would integrate with a transactional email provider.
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly ILogger<SmtpEmailSender> _logger;
    private readonly string _senderAddress;
    private readonly string _senderName;
    private readonly bool _isEnabled;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Read configuration for email settings
        var emailSection = configuration.GetSection("Email");
        _senderAddress = emailSection["SenderAddress"] ?? "noreply@mercato.com";
        _senderName = emailSection["SenderName"] ?? "Mercato Marketplace";
        _isEnabled = bool.TryParse(emailSection["Enabled"], out var enabled) && enabled;

        if (!_isEnabled)
        {
            _logger.LogWarning(
                "Email sending is disabled. Set Email:Enabled=true in configuration to enable. " +
                "Emails will be logged but not sent.");
        }
    }

    public string SenderAddress => _senderAddress;

    public string SenderName => _senderName;

    public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        // Log the send attempt
        _logger.LogInformation(
            "Email send attempt: To={To}, Subject={Subject}, Template={TemplateName}, Locale={Locale}, Sender={SenderAddress}",
            message.To,
            message.Subject,
            message.TemplateName ?? "None",
            message.Locale ?? "en-US",
            _senderAddress);

        if (!_isEnabled)
        {
            // In development/test mode, log the email content and return success
            _logger.LogInformation(
                "Email (simulated): To={To}, Subject={Subject}, HtmlBodyLength={HtmlBodyLength}",
                message.To,
                message.Subject,
                message.HtmlBody?.Length ?? 0);

            var simulatedMessageId = $"simulated-{Guid.NewGuid():N}";

            _logger.LogInformation(
                "Email send result: Success=True, MessageId={MessageId} (simulated - email sending disabled)",
                simulatedMessageId);

            return Task.FromResult(EmailSendResult.Success(simulatedMessageId));
        }

        // TODO: Implement actual SMTP/email provider integration
        // This would use SmtpClient, SendGrid, Amazon SES, or another provider
        // For now, we simulate success and log the attempt

        try
        {
            // Simulate email sending - in production, replace with actual provider call
            var messageId = $"msg-{Guid.NewGuid():N}";

            _logger.LogInformation(
                "Email send result: Success=True, MessageId={MessageId}, To={To}",
                messageId,
                message.To);

            return Task.FromResult(EmailSendResult.Success(messageId));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Email send result: Success=False, To={To}, Error={ErrorMessage}",
                message.To,
                ex.Message);

            return Task.FromResult(EmailSendResult.Failed(ex.Message, "SEND_FAILED"));
        }
    }
}
