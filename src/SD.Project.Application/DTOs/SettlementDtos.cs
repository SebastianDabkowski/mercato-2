namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO for settlement information.
/// </summary>
public sealed record SettlementDto(
    Guid Id,
    Guid StoreId,
    Guid SellerId,
    string StoreName,
    int Year,
    int Month,
    string SettlementNumber,
    string Status,
    string Currency,
    decimal GrossSales,
    decimal TotalShipping,
    decimal TotalCommission,
    decimal TotalRefunds,
    decimal TotalAdjustments,
    decimal NetPayable,
    int OrderCount,
    int Version,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    DateTime CreatedAt,
    DateTime? FinalizedAt,
    DateTime? ApprovedAt,
    DateTime? ExportedAt,
    string? ApprovedBy,
    string? Notes);

/// <summary>
/// DTO for settlement list item.
/// </summary>
public sealed record SettlementListItemDto(
    Guid Id,
    Guid StoreId,
    string StoreName,
    int Year,
    int Month,
    string SettlementNumber,
    string Status,
    string Currency,
    decimal NetPayable,
    int OrderCount,
    int Version,
    DateTime CreatedAt,
    DateTime? FinalizedAt);

/// <summary>
/// DTO for detailed settlement view with items and adjustments.
/// </summary>
public sealed record SettlementDetailsDto(
    Guid Id,
    Guid StoreId,
    Guid SellerId,
    string StoreName,
    int Year,
    int Month,
    string SettlementNumber,
    string Status,
    string Currency,
    decimal GrossSales,
    decimal TotalShipping,
    decimal TotalCommission,
    decimal TotalRefunds,
    decimal TotalAdjustments,
    decimal NetPayable,
    int OrderCount,
    int Version,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    IReadOnlyList<SettlementItemDto> Items,
    IReadOnlyList<SettlementAdjustmentDto> Adjustments,
    DateTime CreatedAt,
    DateTime? FinalizedAt,
    DateTime? ApprovedAt,
    DateTime? ExportedAt,
    string? ApprovedBy,
    string? Notes);

/// <summary>
/// DTO for settlement item.
/// </summary>
public sealed record SettlementItemDto(
    Guid Id,
    Guid EscrowAllocationId,
    Guid ShipmentId,
    string? OrderNumber,
    decimal SellerAmount,
    decimal ShippingAmount,
    decimal CommissionAmount,
    decimal RefundedAmount,
    decimal NetAmount,
    DateTime TransactionDate);

/// <summary>
/// DTO for settlement adjustment.
/// </summary>
public sealed record SettlementAdjustmentDto(
    Guid Id,
    int OriginalYear,
    int OriginalMonth,
    decimal Amount,
    string Reason,
    Guid? RelatedOrderId,
    string? RelatedOrderNumber,
    DateTime CreatedAt);

/// <summary>
/// Result DTO for generate settlement operations.
/// </summary>
public sealed record GenerateSettlementResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    Guid? SettlementId,
    string? SettlementNumber,
    decimal? NetPayable,
    int ItemCount)
{
    public static GenerateSettlementResultDto Succeeded(
        Guid settlementId,
        string settlementNumber,
        decimal netPayable,
        int itemCount) =>
        new(true, null, settlementId, settlementNumber, netPayable, itemCount);

    public static GenerateSettlementResultDto Failed(string errorMessage) =>
        new(false, errorMessage, null, null, null, 0);

    public static GenerateSettlementResultDto NoData() =>
        new(false, "No escrow activity found for the specified period.", null, null, null, 0);
}

/// <summary>
/// Result DTO for finalize settlement operations.
/// </summary>
public sealed record FinalizeSettlementResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    Guid? SettlementId)
{
    public static FinalizeSettlementResultDto Succeeded(Guid settlementId) =>
        new(true, null, settlementId);

    public static FinalizeSettlementResultDto Failed(string errorMessage) =>
        new(false, errorMessage, null);
}

/// <summary>
/// Result DTO for approve settlement operations.
/// </summary>
public sealed record ApproveSettlementResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    Guid? SettlementId)
{
    public static ApproveSettlementResultDto Succeeded(Guid settlementId) =>
        new(true, null, settlementId);

    public static ApproveSettlementResultDto Failed(string errorMessage) =>
        new(false, errorMessage, null);
}

/// <summary>
/// DTO for settlement export data (CSV format).
/// </summary>
public sealed record SettlementExportDto(
    string SettlementNumber,
    string StoreName,
    string Period,
    string Currency,
    decimal GrossSales,
    decimal TotalShipping,
    decimal TotalCommission,
    decimal TotalRefunds,
    decimal TotalAdjustments,
    decimal NetPayable,
    int OrderCount,
    string Status,
    DateTime GeneratedAt);

/// <summary>
/// DTO for settlement item export data (CSV format).
/// </summary>
public sealed record SettlementItemExportDto(
    string SettlementNumber,
    string OrderNumber,
    decimal SellerAmount,
    decimal ShippingAmount,
    decimal CommissionAmount,
    decimal RefundedAmount,
    decimal NetAmount,
    DateTime TransactionDate);

/// <summary>
/// Configuration for settlement calendar rules.
/// </summary>
public sealed record SettlementCalendarConfigDto(
    int GenerationDayOfMonth,
    int FinalizationDayOfMonth,
    bool AutoFinalize,
    bool IncludeCurrentMonth);

/// <summary>
/// Summary DTO for settlements overview.
/// </summary>
public sealed record SettlementsSummaryDto(
    int TotalSettlements,
    int DraftCount,
    int FinalizedCount,
    int ApprovedCount,
    int ExportedCount,
    decimal TotalNetPayable,
    string Currency);
