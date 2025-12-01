namespace SD.Project.Application.Interfaces;

/// <summary>
/// Represents the result of an email send attempt.
/// </summary>
/// <param name="IsSuccess">Whether the email was sent successfully.</param>
/// <param name="MessageId">The unique identifier assigned by the email provider (if successful).</param>
/// <param name="ErrorMessage">Error message if the send failed.</param>
/// <param name="ErrorCode">Error code from the email provider (if available).</param>
public record EmailSendResult(
    bool IsSuccess,
    string? MessageId = null,
    string? ErrorMessage = null,
    string? ErrorCode = null)
{
    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static EmailSendResult Success(string messageId) => new(true, messageId);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static EmailSendResult Failed(string errorMessage, string? errorCode = null)
        => new(false, null, errorMessage, errorCode);
}

/// <summary>
/// Represents an email message to be sent.
/// </summary>
/// <param name="To">The recipient email address.</param>
/// <param name="Subject">The email subject line.</param>
/// <param name="HtmlBody">The HTML body content.</param>
/// <param name="TextBody">Optional plain text body for email clients that don't support HTML.</param>
/// <param name="TemplateName">The name of the template used (for logging/tracking).</param>
/// <param name="Locale">The locale used for localization (e.g., "en-US", "pl-PL").</param>
public record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string? TextBody = null,
    string? TemplateName = null,
    string? Locale = null);

/// <summary>
/// Abstraction for sending transactional emails.
/// Implementations can integrate with email providers like SendGrid, Amazon SES, etc.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends an email message.
    /// </summary>
    /// <param name="message">The email message to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the send attempt.</returns>
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the configured sender email address.
    /// </summary>
    string SenderAddress { get; }

    /// <summary>
    /// Gets the configured sender display name.
    /// </summary>
    string SenderName { get; }
}
