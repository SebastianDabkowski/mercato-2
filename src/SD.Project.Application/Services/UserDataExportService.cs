using System.Text.Json;
using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for managing GDPR user data exports (right of access).
/// </summary>
public sealed class UserDataExportService
{
    private const string ExportVersion = "1.0";
    private const string DataScope = "Full user data export including profile, orders, addresses, reviews, consents, and login history";
    private const int LoginEventHistoryCount = 50;

    private readonly IUserDataExportRepository _exportRepository;
    private readonly IUserRepository _userRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IDeliveryAddressRepository _addressRepository;
    private readonly IReviewRepository _reviewRepository;
    private readonly IUserConsentRepository _consentRepository;
    private readonly ILoginEventRepository _loginEventRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IPayoutSettingsRepository _payoutSettingsRepository;
    private readonly IAuditLoggingService _auditLoggingService;

    public UserDataExportService(
        IUserDataExportRepository exportRepository,
        IUserRepository userRepository,
        IOrderRepository orderRepository,
        IDeliveryAddressRepository addressRepository,
        IReviewRepository reviewRepository,
        IUserConsentRepository consentRepository,
        ILoginEventRepository loginEventRepository,
        IStoreRepository storeRepository,
        IPayoutSettingsRepository payoutSettingsRepository,
        IAuditLoggingService auditLoggingService)
    {
        _exportRepository = exportRepository;
        _userRepository = userRepository;
        _orderRepository = orderRepository;
        _addressRepository = addressRepository;
        _reviewRepository = reviewRepository;
        _consentRepository = consentRepository;
        _loginEventRepository = loginEventRepository;
        _storeRepository = storeRepository;
        _payoutSettingsRepository = payoutSettingsRepository;
        _auditLoggingService = auditLoggingService;
    }

    /// <summary>
    /// Handles a request to export user data.
    /// </summary>
    public async Task<UserDataExportRequestResultDto> HandleAsync(
        RequestUserDataExportCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Validate user exists
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return UserDataExportRequestResultDto.Failed("User not found.");
        }

        // Check if user has a recent pending export to prevent abuse
        var hasPendingExport = await _exportRepository.HasRecentPendingExportAsync(
            command.UserId, withinHours: 24, cancellationToken);

        if (hasPendingExport)
        {
            return UserDataExportRequestResultDto.Failed(
                "You already have a pending data export request. Please wait for it to complete before requesting another.");
        }

        // Create export request
        var export = new UserDataExport(command.UserId, command.IpAddress, command.UserAgent);
        await _exportRepository.AddAsync(export, cancellationToken);
        await _exportRepository.SaveChangesAsync(cancellationToken);

        // Process the export immediately (synchronous for this implementation)
        // For large datasets, this could be made asynchronous with a background job
        try
        {
            export.StartProcessing();
            _exportRepository.Update(export);
            await _exportRepository.SaveChangesAsync(cancellationToken);

            var exportedData = await GenerateExportDataAsync(command.UserId, export.Id, cancellationToken);
            var jsonData = JsonSerializer.Serialize(exportedData, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            export.Complete(jsonData, expirationHours: 72);
            _exportRepository.Update(export);
            await _exportRepository.SaveChangesAsync(cancellationToken);

            // Log the export for audit purposes
            await _auditLoggingService.LogSensitiveAccessAsync(
                accessedByUserId: command.UserId,
                accessedByRole: user.Role,
                resourceType: SensitiveResourceType.CustomerProfile,
                resourceId: command.UserId,
                action: SensitiveAccessAction.Export,
                resourceOwnerId: command.UserId,
                accessReason: "GDPR data export request",
                ipAddress: command.IpAddress,
                userAgent: command.UserAgent,
                cancellationToken: cancellationToken);

            return UserDataExportRequestResultDto.Succeeded(
                MapToDto(export),
                "Your data export has been generated and is ready for download.");
        }
        catch (Exception ex)
        {
            export.Fail(ex.Message);
            _exportRepository.Update(export);
            await _exportRepository.SaveChangesAsync(cancellationToken);

            return UserDataExportRequestResultDto.Failed($"Failed to generate data export: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all data exports for a user.
    /// </summary>
    public async Task<IReadOnlyCollection<UserDataExportDto>> HandleAsync(
        GetUserDataExportsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var exports = await _exportRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        return exports.Select(MapToDto).ToArray();
    }

    /// <summary>
    /// Gets a specific data export by ID.
    /// </summary>
    public async Task<UserDataExportDto?> HandleAsync(
        GetUserDataExportByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var export = await _exportRepository.GetByIdAsync(query.ExportId, cancellationToken);
        if (export is null || export.UserId != query.UserId)
        {
            return null;
        }

        return MapToDto(export);
    }

    /// <summary>
    /// Downloads a data export.
    /// </summary>
    public async Task<UserDataExportDownloadDto?> HandleAsync(
        DownloadUserDataExportQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var export = await _exportRepository.GetByIdAsync(query.ExportId, cancellationToken);
        if (export is null || export.UserId != query.UserId)
        {
            return null;
        }

        if (!export.IsDownloadable || string.IsNullOrEmpty(export.ExportData))
        {
            return null;
        }

        var fileName = $"mercato-data-export-{export.RequestedAt:yyyyMMdd-HHmmss}.json";

        return new UserDataExportDownloadDto(
            export.Id,
            export.UserId,
            export.CompletedAt ?? export.RequestedAt,
            export.ExportData,
            fileName);
    }

    private async Task<ExportedUserDataDto> GenerateExportDataAsync(
        Guid userId,
        Guid exportId,
        CancellationToken cancellationToken)
    {
        // Get user profile
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new InvalidOperationException("User not found.");
        }

        var profile = new ExportedUserProfileDto(
            UserId: user.Id,
            Email: user.Email.Value,
            FirstName: user.FirstName,
            LastName: user.LastName,
            CompanyName: user.CompanyName,
            TaxId: user.TaxId,
            PhoneNumber: user.PhoneNumber,
            Role: user.Role.ToString(),
            Status: user.Status.ToString(),
            IsEmailVerified: user.IsEmailVerified,
            EmailVerifiedAt: user.EmailVerifiedAt,
            TwoFactorEnabled: user.TwoFactorEnabled,
            CreatedAt: user.CreatedAt);

        // Get orders
        var orders = await _orderRepository.GetByBuyerIdAsync(userId, cancellationToken);
        var exportedOrders = orders.Select(o => new ExportedOrderDto(
            OrderId: o.Id,
            OrderNumber: o.OrderNumber,
            Status: o.Status.ToString(),
            CreatedAt: o.CreatedAt,
            TotalAmount: o.TotalAmount,
            Currency: o.Currency,
            DeliveryAddress: new ExportedOrderAddressDto(
                RecipientName: o.RecipientName,
                Street: o.DeliveryStreet,
                Street2: o.DeliveryStreet2,
                City: o.DeliveryCity,
                State: o.DeliveryState,
                PostalCode: o.DeliveryPostalCode,
                Country: o.DeliveryCountry,
                PhoneNumber: o.DeliveryPhoneNumber),
            Items: o.Items.Select(i => new ExportedOrderItemDto(
                ProductName: i.ProductName,
                UnitPrice: i.UnitPrice,
                Quantity: i.Quantity,
                LineTotal: i.LineTotal)).ToArray()
        )).ToArray();

        // Get delivery addresses
        var addresses = await _addressRepository.GetByBuyerIdAsync(userId, cancellationToken);
        var exportedAddresses = addresses.Select(a => new ExportedDeliveryAddressDto(
            AddressId: a.Id,
            Label: a.Label ?? "Address",
            RecipientName: a.RecipientName,
            Street: a.Street,
            Street2: a.Street2,
            City: a.City,
            State: a.State,
            PostalCode: a.PostalCode,
            Country: a.Country,
            PhoneNumber: a.PhoneNumber,
            IsDefault: a.IsDefault)).ToArray();

        // Get reviews
        var reviews = await _reviewRepository.GetByBuyerIdAsync(userId, cancellationToken);
        var exportedReviews = reviews.Select(r => new ExportedReviewDto(
            ReviewId: r.Id,
            ProductId: r.ProductId,
            Rating: r.Rating,
            Comment: r.Comment,
            CreatedAt: r.CreatedAt)).ToArray();

        // Get consents
        var consents = await _consentRepository.GetByUserIdAsync(userId, cancellationToken);
        var exportedConsents = new List<ExportedConsentDto>();
        foreach (var consent in consents)
        {
            var consentType = await _consentRepository.GetConsentTypeByIdAsync(consent.ConsentTypeId, cancellationToken);
            var consentVersion = await _consentRepository.GetConsentVersionByIdAsync(consent.ConsentVersionId, cancellationToken);
            
            exportedConsents.Add(new ExportedConsentDto(
                ConsentType: consentType?.Code ?? "Unknown",
                ConsentTypeName: consentType?.Name ?? "Unknown",
                IsGranted: consent.IsGranted,
                ConsentedAt: consent.ConsentedAt,
                WithdrawnAt: consent.WithdrawnAt,
                ConsentVersion: consentVersion?.Version ?? "Unknown"));
        }

        // Get login history (last 50 events)
        var loginEvents = await _loginEventRepository.GetRecentByUserIdAsync(userId, LoginEventHistoryCount, cancellationToken);
        var exportedLoginEvents = loginEvents.Select(e => new ExportedLoginEventDto(
            EventTime: e.OccurredAt,
            EventType: e.EventType.ToString(),
            IpAddress: e.IpAddress,
            Success: e.IsSuccess)).ToArray();

        // Get seller data if applicable
        ExportedSellerDataDto? sellerData = null;
        if (user.Role == UserRole.Seller)
        {
            var store = await _storeRepository.GetBySellerIdAsync(userId, cancellationToken);
            var payoutSettings = await _payoutSettingsRepository.GetBySellerIdAsync(userId, cancellationToken);

            var payoutSettingsList = new List<ExportedPayoutSettingsDto>();
            if (payoutSettings is not null && payoutSettings.IsConfigured)
            {
                payoutSettingsList.Add(new ExportedPayoutSettingsDto(
                    PayoutMethod: payoutSettings.DefaultPayoutMethod.ToString(),
                    AccountDetails: MaskAccountDetails(payoutSettings),
                    IsDefault: true,
                    CreatedAt: payoutSettings.CreatedAt));
            }

            sellerData = new ExportedSellerDataDto(
                Store: store is not null ? new ExportedStoreDto(
                    StoreId: store.Id,
                    StoreName: store.Name,
                    Description: store.Description,
                    Slug: store.Slug,
                    Status: store.Status.ToString(),
                    CreatedAt: store.CreatedAt) : null,
                PayoutSettings: payoutSettingsList);
        }

        // Create metadata
        var metadata = new ExportMetadataDto(
            ExportId: exportId,
            GeneratedAt: DateTime.UtcNow,
            ExportVersion: ExportVersion,
            DataScope: DataScope);

        return new ExportedUserDataDto(
            Profile: profile,
            Orders: exportedOrders,
            DeliveryAddresses: exportedAddresses,
            Reviews: exportedReviews,
            Consents: exportedConsents,
            LoginHistory: exportedLoginEvents,
            SellerData: sellerData,
            Metadata: metadata);
    }

    private static string? MaskAccountDetails(PayoutSettings settings)
    {
        // Mask sensitive financial information while still providing some identifying info
        if (!string.IsNullOrEmpty(settings.SepaIban))
        {
            return $"IBAN: {MaskString(settings.SepaIban, 4)}";
        }
        if (!string.IsNullOrEmpty(settings.BankAccountNumber))
        {
            return $"Account: {MaskString(settings.BankAccountNumber, 4)}";
        }
        return null;
    }

    private static string MaskString(string value, int visibleChars)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        // For security, always mask at least some characters
        // If value is too short, show only a portion with masking
        if (value.Length <= visibleChars)
        {
            // For short values, mask the first half
            var halfLength = Math.Max(1, value.Length / 2);
            return new string('*', halfLength) + value[halfLength..];
        }
        
        var masked = new string('*', value.Length - visibleChars);
        return masked + value[^visibleChars..];
    }

    private static UserDataExportDto MapToDto(UserDataExport export)
    {
        return new UserDataExportDto(
            Id: export.Id,
            UserId: export.UserId,
            Status: export.Status,
            RequestedAt: export.RequestedAt,
            CompletedAt: export.CompletedAt,
            ExpiresAt: export.ExpiresAt,
            IsDownloadable: export.IsDownloadable,
            ErrorMessage: export.ErrorMessage);
    }
}
