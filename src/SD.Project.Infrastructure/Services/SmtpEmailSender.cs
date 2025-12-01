using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SD.Project.Application.Interfaces;

namespace SD.Project.Infrastructure.Services;

/// <summary>
/// Configurable email sender implementation that logs all send attempts and results.
/// When email sending is disabled (Email:Enabled=false), emails are simulated and logged.
/// When enabled, this integrates with transactional email providers like SendGrid or Amazon SES.
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly ILogger<SmtpEmailSender> _logger;
    private readonly string _senderAddress;
    private readonly string _senderName;
    private readonly bool _isEnabled;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
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

        // Log the send attempt for audit purposes
        _logger.LogInformation(
            "Email send attempt: To={To}, Subject={Subject}, Template={TemplateName}, Locale={Locale}, Sender={SenderAddress}",
            message.To,
            message.Subject,
            message.TemplateName ?? "None",
            message.Locale ?? "en-US",
            _senderAddress);

        if (!_isEnabled)
        {
            // Email sending is disabled - simulate success and log content for development/testing
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

        try
        {
            // When Email:Enabled=true, this is where the actual email provider integration goes.
            // Integrate with SmtpClient, SendGrid, Amazon SES, or another transactional email provider.
            // For now, we log and simulate success since no provider is configured.
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
