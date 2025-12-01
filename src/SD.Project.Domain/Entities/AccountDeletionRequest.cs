namespace SD.Project.Domain.Entities;

/// <summary>
/// Status of an account deletion request.
/// </summary>
public enum AccountDeletionRequestStatus
{
    /// <summary>Request submitted by user, awaiting confirmation.</summary>
    Pending,
    /// <summary>Deletion confirmed and being processed.</summary>
    Processing,
    /// <summary>Account successfully deleted and anonymized.</summary>
    Completed,
    /// <summary>Request was cancelled by the user or blocked.</summary>
    Cancelled,
    /// <summary>Request failed due to blocking conditions.</summary>
    Blocked
}

/// <summary>
/// Represents a request to delete a user account with anonymization.
/// </summary>
public class AccountDeletionRequest
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The ID of the user requesting account deletion.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Current status of the deletion request.
    /// </summary>
    public AccountDeletionRequestStatus Status { get; private set; }

    /// <summary>
    /// IP address from which the request was made.
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// User agent string from which the request was made.
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// Reason for blocking the request, if applicable.
    /// </summary>
    public string? BlockingReason { get; private set; }

    /// <summary>
    /// When the request was created.
    /// </summary>
    public DateTime RequestedAt { get; private set; }

    /// <summary>
    /// When the request was confirmed by the user.
    /// </summary>
    public DateTime? ConfirmedAt { get; private set; }

    /// <summary>
    /// When the account was actually deleted and anonymized.
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// When the request was cancelled or blocked.
    /// </summary>
    public DateTime? CancelledAt { get; private set; }

    private AccountDeletionRequest()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new account deletion request.
    /// </summary>
    /// <param name="userId">The ID of the user requesting deletion.</param>
    /// <param name="ipAddress">The IP address (optional).</param>
    /// <param name="userAgent">The user agent (optional).</param>
    public AccountDeletionRequest(Guid userId, string? ipAddress = null, string? userAgent = null)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        Id = Guid.NewGuid();
        UserId = userId;
        Status = AccountDeletionRequestStatus.Pending;
        IpAddress = ipAddress?.Trim();
        UserAgent = userAgent?.Trim();
        RequestedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Confirms the deletion request and starts processing.
    /// </summary>
    public void Confirm()
    {
        if (Status != AccountDeletionRequestStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot confirm deletion request in status {Status}.");
        }

        Status = AccountDeletionRequestStatus.Processing;
        ConfirmedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the deletion as completed.
    /// </summary>
    public void Complete()
    {
        if (Status != AccountDeletionRequestStatus.Processing)
        {
            throw new InvalidOperationException($"Cannot complete deletion request in status {Status}.");
        }

        Status = AccountDeletionRequestStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancels the deletion request.
    /// </summary>
    public void Cancel()
    {
        if (Status != AccountDeletionRequestStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot cancel deletion request in status {Status}.");
        }

        Status = AccountDeletionRequestStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Blocks the deletion request due to blocking conditions.
    /// </summary>
    /// <param name="reason">The reason for blocking.</param>
    public void Block(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Blocking reason is required.", nameof(reason));
        }

        if (Status != AccountDeletionRequestStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot block deletion request in status {Status}.");
        }

        Status = AccountDeletionRequestStatus.Blocked;
        BlockingReason = reason.Trim();
        CancelledAt = DateTime.UtcNow;
    }
}
