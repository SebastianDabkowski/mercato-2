namespace SD.Project.Application.Commands;

/// <summary>
/// Command to approve a seller rating.
/// </summary>
public record ApproveSellerRatingCommand(
    Guid SellerRatingId,
    Guid ModeratorId,
    string? Notes = null);

/// <summary>
/// Command to reject a seller rating.
/// </summary>
public record RejectSellerRatingCommand(
    Guid SellerRatingId,
    Guid ModeratorId,
    string Reason,
    string? Notes = null);

/// <summary>
/// Command to flag a seller rating for moderation.
/// </summary>
public record FlagSellerRatingCommand(
    Guid SellerRatingId,
    Guid ModeratorId,
    string Reason,
    string? Notes = null);

/// <summary>
/// Command to clear a flag on a seller rating.
/// </summary>
public record ClearSellerRatingFlagCommand(
    Guid SellerRatingId,
    Guid ModeratorId,
    string? Notes = null);

/// <summary>
/// Command to reset a seller rating to pending status for re-review.
/// </summary>
public record ResetSellerRatingToPendingCommand(
    Guid SellerRatingId,
    Guid ModeratorId,
    string Reason,
    string? Notes = null);

/// <summary>
/// Command for batch approval of seller ratings.
/// </summary>
public record BatchApproveSellerRatingsCommand(
    IReadOnlyList<Guid> SellerRatingIds,
    Guid ModeratorId,
    string? Notes = null);

/// <summary>
/// Command for batch rejection of seller ratings.
/// </summary>
public record BatchRejectSellerRatingsCommand(
    IReadOnlyList<Guid> SellerRatingIds,
    Guid ModeratorId,
    string Reason,
    string? Notes = null);
