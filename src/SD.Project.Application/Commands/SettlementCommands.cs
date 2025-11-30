namespace SD.Project.Application.Commands;

/// <summary>
/// Command to generate a monthly settlement for a specific store.
/// </summary>
public sealed record GenerateSettlementCommand(
    Guid StoreId,
    int Year,
    int Month,
    bool Regenerate = false);

/// <summary>
/// Command to generate settlements for all stores for a specific period.
/// </summary>
public sealed record GenerateAllSettlementsCommand(
    int Year,
    int Month,
    bool Regenerate = false);

/// <summary>
/// Command to finalize a settlement.
/// </summary>
public sealed record FinalizeSettlementCommand(Guid SettlementId);

/// <summary>
/// Command to approve a settlement.
/// </summary>
public sealed record ApproveSettlementCommand(
    Guid SettlementId,
    string ApprovedBy);

/// <summary>
/// Command to add an adjustment to a settlement.
/// </summary>
public sealed record AddSettlementAdjustmentCommand(
    Guid SettlementId,
    int OriginalYear,
    int OriginalMonth,
    decimal Amount,
    string Reason,
    Guid? RelatedOrderId = null,
    string? RelatedOrderNumber = null);

/// <summary>
/// Command to update settlement notes.
/// </summary>
public sealed record UpdateSettlementNotesCommand(
    Guid SettlementId,
    string? Notes);

/// <summary>
/// Command to export a settlement.
/// </summary>
public sealed record ExportSettlementCommand(Guid SettlementId);
