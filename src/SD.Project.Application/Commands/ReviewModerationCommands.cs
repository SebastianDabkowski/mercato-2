namespace SD.Project.Application.Commands;

/// <summary>
/// Command to approve a review.
/// </summary>
public record ApproveReviewCommand(
    Guid ReviewId,
    Guid ModeratorId,
    string? Notes = null);

/// <summary>
/// Command to reject a review.
/// </summary>
public record RejectReviewCommand(
    Guid ReviewId,
    Guid ModeratorId,
    string Reason,
    string? Notes = null);

/// <summary>
/// Command to flag a review for moderation.
/// </summary>
public record FlagReviewCommand(
    Guid ReviewId,
    Guid ModeratorId,
    string Reason,
    string? Notes = null);

/// <summary>
/// Command to clear a flag on a review.
/// </summary>
public record ClearReviewFlagCommand(
    Guid ReviewId,
    Guid ModeratorId,
    string? Notes = null);

/// <summary>
/// Command to reset a review to pending status for re-review.
/// </summary>
public record ResetReviewToPendingCommand(
    Guid ReviewId,
    Guid ModeratorId,
    string Reason,
    string? Notes = null);

/// <summary>
/// Command for batch approval of reviews.
/// </summary>
public record BatchApproveReviewsCommand(
    IReadOnlyList<Guid> ReviewIds,
    Guid ModeratorId,
    string? Notes = null);

/// <summary>
/// Command for batch rejection of reviews.
/// </summary>
public record BatchRejectReviewsCommand(
    IReadOnlyList<Guid> ReviewIds,
    Guid ModeratorId,
    string Reason,
    string? Notes = null);
