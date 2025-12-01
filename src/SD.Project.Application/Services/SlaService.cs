using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for SLA configuration and monitoring.
/// Handles SLA configuration CRUD, breach checking, and statistics.
/// </summary>
public sealed class SlaService
{
    private readonly ISlaConfigurationRepository _slaConfigurationRepository;
    private readonly IReturnRequestRepository _returnRequestRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;

    public SlaService(
        ISlaConfigurationRepository slaConfigurationRepository,
        IReturnRequestRepository returnRequestRepository,
        IStoreRepository storeRepository,
        IOrderRepository orderRepository,
        IUserRepository userRepository,
        INotificationService notificationService)
    {
        _slaConfigurationRepository = slaConfigurationRepository;
        _returnRequestRepository = returnRequestRepository;
        _storeRepository = storeRepository;
        _orderRepository = orderRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Gets all SLA configurations.
    /// </summary>
    public async Task<IReadOnlyList<SlaConfigurationDto>> HandleAsync(
        GetSlaConfigurationsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var configurations = await _slaConfigurationRepository.GetAllAsync(cancellationToken);

        return configurations.Select(c => new SlaConfigurationDto(
            c.Id,
            c.Category.ToString(),
            c.FirstResponseHours,
            c.ResolutionHours,
            c.IsEnabled,
            c.Description,
            c.CreatedAt,
            c.UpdatedAt)).ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets a specific SLA configuration by ID.
    /// </summary>
    public async Task<SlaConfigurationDto?> HandleAsync(
        GetSlaConfigurationByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var config = await _slaConfigurationRepository.GetByIdAsync(query.ConfigurationId, cancellationToken);
        if (config is null)
        {
            return null;
        }

        return new SlaConfigurationDto(
            config.Id,
            config.Category.ToString(),
            config.FirstResponseHours,
            config.ResolutionHours,
            config.IsEnabled,
            config.Description,
            config.CreatedAt,
            config.UpdatedAt);
    }

    /// <summary>
    /// Creates a new SLA configuration.
    /// </summary>
    public async Task<SlaConfigurationResultDto> HandleAsync(
        CreateSlaConfigurationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Parse category
        if (!Enum.TryParse<SlaCaseCategory>(command.Category, ignoreCase: true, out var category))
        {
            return new SlaConfigurationResultDto(false, "Invalid category. Must be Default, Return, or Complaint.");
        }

        // Check if configuration already exists for this category
        var existing = await _slaConfigurationRepository.ExistsForCategoryAsync(category, cancellationToken);
        if (existing)
        {
            return new SlaConfigurationResultDto(false, $"An SLA configuration already exists for category '{category}'.");
        }

        try
        {
            var config = new SlaConfiguration(
                category,
                command.FirstResponseHours,
                command.ResolutionHours,
                command.Description);

            await _slaConfigurationRepository.AddAsync(config, cancellationToken);
            await _slaConfigurationRepository.SaveChangesAsync(cancellationToken);

            return new SlaConfigurationResultDto(true, null, config.Id);
        }
        catch (ArgumentException ex)
        {
            return new SlaConfigurationResultDto(false, ex.Message);
        }
    }

    /// <summary>
    /// Updates an existing SLA configuration.
    /// </summary>
    public async Task<SlaConfigurationResultDto> HandleAsync(
        UpdateSlaConfigurationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var config = await _slaConfigurationRepository.GetByIdAsync(command.ConfigurationId, cancellationToken);
        if (config is null)
        {
            return new SlaConfigurationResultDto(false, "SLA configuration not found.");
        }

        try
        {
            config.UpdateThresholds(
                command.FirstResponseHours,
                command.ResolutionHours,
                command.Description);

            await _slaConfigurationRepository.UpdateAsync(config, cancellationToken);
            await _slaConfigurationRepository.SaveChangesAsync(cancellationToken);

            return new SlaConfigurationResultDto(true, null, config.Id);
        }
        catch (ArgumentException ex)
        {
            return new SlaConfigurationResultDto(false, ex.Message);
        }
    }

    /// <summary>
    /// Enables an SLA configuration.
    /// </summary>
    public async Task<SlaConfigurationResultDto> HandleAsync(
        EnableSlaConfigurationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var config = await _slaConfigurationRepository.GetByIdAsync(command.ConfigurationId, cancellationToken);
        if (config is null)
        {
            return new SlaConfigurationResultDto(false, "SLA configuration not found.");
        }

        config.Enable();
        await _slaConfigurationRepository.UpdateAsync(config, cancellationToken);
        await _slaConfigurationRepository.SaveChangesAsync(cancellationToken);

        return new SlaConfigurationResultDto(true, null, config.Id);
    }

    /// <summary>
    /// Disables an SLA configuration.
    /// </summary>
    public async Task<SlaConfigurationResultDto> HandleAsync(
        DisableSlaConfigurationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var config = await _slaConfigurationRepository.GetByIdAsync(command.ConfigurationId, cancellationToken);
        if (config is null)
        {
            return new SlaConfigurationResultDto(false, "SLA configuration not found.");
        }

        config.Disable();
        await _slaConfigurationRepository.UpdateAsync(config, cancellationToken);
        await _slaConfigurationRepository.SaveChangesAsync(cancellationToken);

        return new SlaConfigurationResultDto(true, null, config.Id);
    }

    /// <summary>
    /// Deletes an SLA configuration.
    /// </summary>
    public async Task<SlaConfigurationResultDto> HandleAsync(
        DeleteSlaConfigurationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var config = await _slaConfigurationRepository.GetByIdAsync(command.ConfigurationId, cancellationToken);
        if (config is null)
        {
            return new SlaConfigurationResultDto(false, "SLA configuration not found.");
        }

        await _slaConfigurationRepository.DeleteAsync(config, cancellationToken);
        await _slaConfigurationRepository.SaveChangesAsync(cancellationToken);

        return new SlaConfigurationResultDto(true, null, config.Id);
    }

    /// <summary>
    /// Gets SLA status for a specific case.
    /// </summary>
    public async Task<CaseSlaStatusDto?> HandleAsync(
        GetCaseSlaStatusQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var returnRequest = await _returnRequestRepository.GetByIdAsync(query.ReturnRequestId, cancellationToken);
        if (returnRequest is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var isFirstResponseOverdue = returnRequest.IsFirstResponseSlaBreached(now);
        var isResolutionOverdue = returnRequest.IsResolutionSlaBreached(now);

        TimeSpan? timeToFirstResponseDeadline = null;
        TimeSpan? timeToResolutionDeadline = null;

        if (returnRequest.FirstResponseDeadline.HasValue && !returnRequest.FirstRespondedAt.HasValue)
        {
            timeToFirstResponseDeadline = returnRequest.FirstResponseDeadline.Value - now;
        }

        if (returnRequest.ResolutionDeadline.HasValue && returnRequest.Status != ReturnRequestStatus.Completed)
        {
            timeToResolutionDeadline = returnRequest.ResolutionDeadline.Value - now;
        }

        return new CaseSlaStatusDto(
            returnRequest.Id,
            returnRequest.CaseNumber,
            returnRequest.FirstResponseDeadline,
            returnRequest.ResolutionDeadline,
            returnRequest.FirstRespondedAt,
            returnRequest.SlaBreached,
            returnRequest.SlaBreachedAt,
            returnRequest.SlaBreachType?.ToString(),
            isFirstResponseOverdue,
            isResolutionOverdue,
            timeToFirstResponseDeadline,
            timeToResolutionDeadline);
    }

    /// <summary>
    /// Checks for SLA breaches and flags cases that have exceeded deadlines.
    /// Also sends notifications and triggers soft escalation.
    /// </summary>
    public async Task<SlaBreachCheckResultDto> HandleAsync(
        CheckSlaBreachesCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var pendingCases = await _returnRequestRepository.GetCasesPendingSlaBreachCheckAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var breachedCaseIds = new List<Guid>();

        foreach (var request in pendingCases)
        {
            var breachDetected = false;
            SlaBreachType? breachType = null;

            // Check first response SLA
            if (request.IsFirstResponseSlaBreached(now))
            {
                breachDetected = true;
                breachType = Domain.Entities.SlaBreachType.FirstResponse;
            }
            // Check resolution SLA
            else if (request.IsResolutionSlaBreached(now))
            {
                breachDetected = true;
                breachType = Domain.Entities.SlaBreachType.Resolution;
            }

            if (breachDetected && breachType.HasValue)
            {
                request.MarkSlaBreached(breachType.Value);
                await _returnRequestRepository.UpdateAsync(request, cancellationToken);
                breachedCaseIds.Add(request.Id);

                // Send notification to seller
                await SendSlaBreachNotificationAsync(request, breachType.Value, cancellationToken);

                // Auto-escalate due to SLA breach if not already escalated
                if (request.CanEscalate())
                {
                    request.Escalate(Guid.Empty, EscalationReason.SLABreach, $"Automatic escalation due to {breachType} SLA breach.");
                    await _returnRequestRepository.UpdateAsync(request, cancellationToken);
                }
            }
        }

        await _returnRequestRepository.SaveChangesAsync(cancellationToken);

        return new SlaBreachCheckResultDto(
            pendingCases.Count,
            breachedCaseIds.Count,
            breachedCaseIds.AsReadOnly());
    }

    /// <summary>
    /// Gets SLA statistics for a specific seller/store.
    /// </summary>
    public async Task<SellerSlaStatisticsDto?> HandleAsync(
        GetSellerSlaStatisticsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var store = await _storeRepository.GetByIdAsync(query.StoreId, cancellationToken);
        if (store is null)
        {
            return null;
        }

        var fromDate = query.FromDate ?? DateTime.UtcNow.AddDays(-30);
        var toDate = query.ToDate ?? DateTime.UtcNow;

        var (totalCases, casesWithSla, casesResolvedWithinSla, firstResponseBreaches, resolutionBreaches, avgFirstResponseTime, avgResolutionTime) =
            await _returnRequestRepository.GetStoreSlaStatisticsAsync(query.StoreId, fromDate, toDate, cancellationToken);

        var percentageResolvedWithinSla = casesWithSla > 0
            ? (decimal)casesResolvedWithinSla / casesWithSla * 100
            : 0m;

        return new SellerSlaStatisticsDto(
            query.StoreId,
            store.Name,
            totalCases,
            casesWithSla,
            casesResolvedWithinSla,
            firstResponseBreaches + resolutionBreaches,
            firstResponseBreaches,
            resolutionBreaches,
            Math.Round(percentageResolvedWithinSla, 2),
            avgFirstResponseTime,
            avgResolutionTime);
    }

    /// <summary>
    /// Gets the SLA dashboard with aggregate statistics.
    /// </summary>
    public async Task<SlaDashboardDto> HandleAsync(
        GetSlaDashboardQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var fromDate = query.FromDate ?? DateTime.UtcNow.AddDays(-30);
        var toDate = query.ToDate ?? DateTime.UtcNow;

        // Get aggregate statistics
        var (totalCases, casesWithSla, casesResolvedWithinSla, totalBreaches, avgFirstResponseTime, avgResolutionTime) =
            await _returnRequestRepository.GetAggregateSlaStatisticsAsync(fromDate, toDate, cancellationToken);

        var overallSlaComplianceRate = casesWithSla > 0
            ? (decimal)casesResolvedWithinSla / casesWithSla * 100
            : 0m;

        // Get recent breaches
        var (recentBreaches, _) = await _returnRequestRepository.GetSlaBreachedCasesAsync(
            null, null, fromDate, toDate, 0, 10, cancellationToken);

        var recentBreachSummaries = new List<SlaBreachedCaseSummaryDto>();
        foreach (var breach in recentBreaches)
        {
            var store = await _storeRepository.GetByIdAsync(breach.StoreId, cancellationToken);
            var daysOverdue = breach.SlaBreachedAt.HasValue
                ? (int)(DateTime.UtcNow - breach.SlaBreachedAt.Value).TotalDays
                : 0;

            recentBreachSummaries.Add(new SlaBreachedCaseSummaryDto(
                breach.Id,
                breach.CaseNumber,
                store?.Name ?? "Unknown Store",
                breach.SlaBreachType?.ToString() ?? "Unknown",
                breach.SlaBreachedAt ?? DateTime.UtcNow,
                breach.Status.ToString(),
                daysOverdue));
        }

        // Get top seller statistics
        // For now, we'll return empty list - in production, this would query stores with cases
        var sellerStatistics = new List<SellerSlaStatisticsDto>();

        return new SlaDashboardDto(
            fromDate,
            toDate,
            totalCases,
            casesWithSla,
            casesResolvedWithinSla,
            totalBreaches,
            Math.Round(overallSlaComplianceRate, 2),
            avgFirstResponseTime,
            avgResolutionTime,
            sellerStatistics.AsReadOnly(),
            recentBreachSummaries.AsReadOnly());
    }

    /// <summary>
    /// Gets cases that have breached SLA.
    /// </summary>
    public async Task<PagedResultDto<SlaBreachedCaseSummaryDto>> HandleAsync(
        GetSlaBreachedCasesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var skip = (pageNumber - 1) * pageSize;

        // Parse breach type filter
        SlaBreachType? breachTypeFilter = null;
        if (!string.IsNullOrWhiteSpace(query.BreachType) &&
            Enum.TryParse<SlaBreachType>(query.BreachType, ignoreCase: true, out var parsedBreachType))
        {
            breachTypeFilter = parsedBreachType;
        }

        var (breachedCases, totalCount) = await _returnRequestRepository.GetSlaBreachedCasesAsync(
            query.StoreId,
            breachTypeFilter,
            query.FromDate,
            query.ToDate,
            skip,
            pageSize,
            cancellationToken);

        var result = new List<SlaBreachedCaseSummaryDto>();
        foreach (var breach in breachedCases)
        {
            var store = await _storeRepository.GetByIdAsync(breach.StoreId, cancellationToken);
            var daysOverdue = breach.SlaBreachedAt.HasValue
                ? (int)(DateTime.UtcNow - breach.SlaBreachedAt.Value).TotalDays
                : 0;

            result.Add(new SlaBreachedCaseSummaryDto(
                breach.Id,
                breach.CaseNumber,
                store?.Name ?? "Unknown Store",
                breach.SlaBreachType?.ToString() ?? "Unknown",
                breach.SlaBreachedAt ?? DateTime.UtcNow,
                breach.Status.ToString(),
                daysOverdue));
        }

        return PagedResultDto<SlaBreachedCaseSummaryDto>.Create(
            result.AsReadOnly(),
            pageNumber,
            pageSize,
            totalCount);
    }

    /// <summary>
    /// Gets the effective SLA configuration for a case type.
    /// </summary>
    public async Task<SlaConfiguration?> GetEffectiveSlaConfigurationAsync(
        ReturnRequestType caseType,
        CancellationToken cancellationToken = default)
    {
        var category = caseType == ReturnRequestType.Return
            ? SlaCaseCategory.Return
            : SlaCaseCategory.Complaint;

        return await _slaConfigurationRepository.GetEffectiveConfigAsync(category, cancellationToken);
    }

    /// <summary>
    /// Applies SLA deadlines to a case based on configuration.
    /// Called when a new case is created.
    /// </summary>
    public async Task ApplySlaDeadlinesAsync(
        ReturnRequest request,
        CancellationToken cancellationToken = default)
    {
        var config = await GetEffectiveSlaConfigurationAsync(request.Type, cancellationToken);
        if (config is null || !config.IsEnabled)
        {
            return; // No SLA configuration or disabled
        }

        var firstResponseDeadline = config.CalculateFirstResponseDeadline(request.CreatedAt);
        var resolutionDeadline = config.CalculateResolutionDeadline(request.CreatedAt);

        request.SetSlaDeadlines(firstResponseDeadline, resolutionDeadline);
    }

    private async Task SendSlaBreachNotificationAsync(
        ReturnRequest request,
        SlaBreachType breachType,
        CancellationToken cancellationToken)
    {
        var store = await _storeRepository.GetByIdAsync(request.StoreId, cancellationToken);
        if (store is null)
        {
            return;
        }

        var seller = await _userRepository.GetByIdAsync(store.SellerId, cancellationToken);
        if (seller?.Email is null)
        {
            return;
        }

        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        var orderNumber = order?.OrderNumber ?? "Unknown";

        var deadline = breachType == SlaBreachType.FirstResponse
            ? request.FirstResponseDeadline!.Value
            : request.ResolutionDeadline!.Value;

        await _notificationService.SendSlaBreachNotificationAsync(
            request.Id,
            request.CaseNumber,
            orderNumber,
            seller.Email.Value,
            breachType.ToString(),
            deadline,
            cancellationToken);
    }
}
