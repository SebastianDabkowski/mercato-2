using SD.Project.Domain.Entities;

namespace SD.Project.Application.DTOs;

/// <summary>
/// Data transfer object for a user data export request.
/// </summary>
/// <param name="Id">The unique identifier of the export.</param>
/// <param name="UserId">The ID of the user who requested the export.</param>
/// <param name="Status">The current status of the export.</param>
/// <param name="RequestedAt">When the export was requested.</param>
/// <param name="CompletedAt">When the export was completed (if applicable).</param>
/// <param name="ExpiresAt">When the export expires and can no longer be downloaded.</param>
/// <param name="IsDownloadable">Whether the export can be downloaded.</param>
/// <param name="ErrorMessage">Error message if the export failed.</param>
public record UserDataExportDto(
    Guid Id,
    Guid UserId,
    UserDataExportStatus Status,
    DateTime RequestedAt,
    DateTime? CompletedAt,
    DateTime? ExpiresAt,
    bool IsDownloadable,
    string? ErrorMessage);

/// <summary>
/// Result of requesting a data export.
/// </summary>
/// <param name="Success">Whether the request was successful.</param>
/// <param name="Export">The created export request (if successful).</param>
/// <param name="Message">A descriptive message.</param>
/// <param name="Errors">List of errors if unsuccessful.</param>
public record UserDataExportRequestResultDto(
    bool Success,
    UserDataExportDto? Export,
    string? Message,
    IReadOnlyList<string> Errors)
{
    public static UserDataExportRequestResultDto Succeeded(UserDataExportDto export, string message) =>
        new(true, export, message, Array.Empty<string>());

    public static UserDataExportRequestResultDto Failed(string error) =>
        new(false, null, null, new[] { error });

    public static UserDataExportRequestResultDto Failed(IReadOnlyList<string> errors) =>
        new(false, null, null, errors);
}

/// <summary>
/// DTO containing the full exported user data.
/// </summary>
/// <param name="ExportId">The ID of the export.</param>
/// <param name="UserId">The ID of the user whose data was exported.</param>
/// <param name="GeneratedAt">When the export was generated.</param>
/// <param name="ExportData">The JSON string containing the exported data.</param>
/// <param name="FileName">The suggested filename for the download.</param>
public record UserDataExportDownloadDto(
    Guid ExportId,
    Guid UserId,
    DateTime GeneratedAt,
    string ExportData,
    string FileName);

/// <summary>
/// Structure representing the complete exported user data.
/// </summary>
public record ExportedUserDataDto(
    ExportedUserProfileDto Profile,
    IReadOnlyList<ExportedOrderDto> Orders,
    IReadOnlyList<ExportedDeliveryAddressDto> DeliveryAddresses,
    IReadOnlyList<ExportedReviewDto> Reviews,
    IReadOnlyList<ExportedConsentDto> Consents,
    IReadOnlyList<ExportedLoginEventDto> LoginHistory,
    ExportedSellerDataDto? SellerData,
    ExportMetadataDto Metadata);

/// <summary>
/// User profile data for export.
/// </summary>
public record ExportedUserProfileDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string? CompanyName,
    string? TaxId,
    string? PhoneNumber,
    string Role,
    string Status,
    bool IsEmailVerified,
    DateTime? EmailVerifiedAt,
    bool TwoFactorEnabled,
    DateTime CreatedAt);

/// <summary>
/// Order data for export.
/// </summary>
public record ExportedOrderDto(
    Guid OrderId,
    string OrderNumber,
    string Status,
    DateTime CreatedAt,
    decimal TotalAmount,
    string Currency,
    ExportedOrderAddressDto DeliveryAddress,
    IReadOnlyList<ExportedOrderItemDto> Items);

/// <summary>
/// Order address for export.
/// </summary>
public record ExportedOrderAddressDto(
    string RecipientName,
    string Street,
    string? Street2,
    string City,
    string? State,
    string PostalCode,
    string Country,
    string? PhoneNumber);

/// <summary>
/// Order item for export.
/// </summary>
public record ExportedOrderItemDto(
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

/// <summary>
/// Delivery address for export.
/// </summary>
public record ExportedDeliveryAddressDto(
    Guid AddressId,
    string Label,
    string RecipientName,
    string Street,
    string? Street2,
    string City,
    string? State,
    string PostalCode,
    string Country,
    string? PhoneNumber,
    bool IsDefault);

/// <summary>
/// Review data for export.
/// </summary>
public record ExportedReviewDto(
    Guid ReviewId,
    Guid ProductId,
    int Rating,
    string? Comment,
    DateTime CreatedAt);

/// <summary>
/// Consent data for export.
/// </summary>
public record ExportedConsentDto(
    string ConsentType,
    string ConsentTypeName,
    bool IsGranted,
    DateTime? ConsentedAt,
    DateTime? WithdrawnAt,
    string ConsentVersion);

/// <summary>
/// Login event for export.
/// </summary>
public record ExportedLoginEventDto(
    DateTime EventTime,
    string EventType,
    string? IpAddress,
    bool Success);

/// <summary>
/// Seller-specific data for export (only for seller users).
/// </summary>
public record ExportedSellerDataDto(
    ExportedStoreDto? Store,
    IReadOnlyList<ExportedPayoutSettingsDto> PayoutSettings);

/// <summary>
/// Store data for export.
/// </summary>
public record ExportedStoreDto(
    Guid StoreId,
    string StoreName,
    string? Description,
    string Slug,
    string Status,
    DateTime CreatedAt);

/// <summary>
/// Payout settings for export.
/// </summary>
public record ExportedPayoutSettingsDto(
    string PayoutMethod,
    string? AccountDetails,
    bool IsDefault,
    DateTime CreatedAt);

/// <summary>
/// Metadata about the export.
/// </summary>
public record ExportMetadataDto(
    Guid ExportId,
    DateTime GeneratedAt,
    string ExportVersion,
    string DataScope);
