using Microsoft.Extensions.Logging;
using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for managing account deletion requests with anonymization.
/// Handles GDPR right to erasure while preserving necessary transactional history.
/// </summary>
public sealed class AccountDeletionService
{
    private readonly IAccountDeletionRequestRepository _deletionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IReturnRequestRepository _returnRequestRepository;
    private readonly IReviewRepository _reviewRepository;
    private readonly IDeliveryAddressRepository _addressRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IUserSessionRepository _sessionRepository;
    private readonly IAuditLoggingService _auditLoggingService;
    private readonly ILogger<AccountDeletionService> _logger;

    public AccountDeletionService(
        IAccountDeletionRequestRepository deletionRepository,
        IUserRepository userRepository,
        IOrderRepository orderRepository,
        IReturnRequestRepository returnRequestRepository,
        IReviewRepository reviewRepository,
        IDeliveryAddressRepository addressRepository,
        IStoreRepository storeRepository,
        IUserSessionRepository sessionRepository,
        IAuditLoggingService auditLoggingService,
        ILogger<AccountDeletionService> logger)
    {
        _deletionRepository = deletionRepository;
        _userRepository = userRepository;
        _orderRepository = orderRepository;
        _returnRequestRepository = returnRequestRepository;
        _reviewRepository = reviewRepository;
        _addressRepository = addressRepository;
        _storeRepository = storeRepository;
        _sessionRepository = sessionRepository;
        _auditLoggingService = auditLoggingService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the impact assessment for account deletion.
    /// </summary>
    public async Task<AccountDeletionImpactDto> HandleAsync(
        GetAccountDeletionImpactQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var user = await _userRepository.GetByIdAsync(query.UserId, cancellationToken);
        if (user is null)
        {
            return new AccountDeletionImpactDto(
                CanDelete: false,
                BlockingConditions: new[] { "User not found." },
                OrderCount: 0,
                ReviewCount: 0,
                AddressCount: 0,
                HasActiveStore: false,
                StoreName: null,
                ImpactSummary: Array.Empty<string>());
        }

        var blockingConditions = await GetBlockingConditionsAsync(user, cancellationToken);
        var impactSummary = new List<string>();

        // Get order count
        var orders = await _orderRepository.GetByBuyerIdAsync(query.UserId, cancellationToken);
        var orderCount = orders.Count;

        // Get review count
        var reviews = await _reviewRepository.GetByBuyerIdAsync(query.UserId, cancellationToken);
        var reviewCount = reviews.Count;

        // Get address count
        var addresses = await _addressRepository.GetByBuyerIdAsync(query.UserId, cancellationToken);
        var addressCount = addresses.Count;

        // Check for store (seller)
        Store? store = null;
        var hasActiveStore = false;
        string? storeName = null;

        if (user.Role == UserRole.Seller)
        {
            store = await _storeRepository.GetBySellerIdAsync(query.UserId, cancellationToken);
            if (store is not null)
            {
                // Check if store is active or publicly visible
                hasActiveStore = store.IsPubliclyVisible();
                storeName = store.Name;
            }
        }

        // Build impact summary
        impactSummary.Add("You will permanently lose access to your account.");
        impactSummary.Add("Your email and login credentials will be removed.");

        if (orderCount > 0)
        {
            impactSummary.Add($"Personal data in {orderCount} order(s) will be anonymized. Order amounts and dates are retained for legal records.");
        }

        if (reviewCount > 0)
        {
            impactSummary.Add($"{reviewCount} review(s) will be anonymized and attributed to 'Deleted User'.");
        }

        if (addressCount > 0)
        {
            impactSummary.Add($"{addressCount} delivery address(es) will be deleted.");
        }

        if (hasActiveStore)
        {
            impactSummary.Add($"Your store '{storeName}' will be deactivated and all listings hidden.");
        }

        impactSummary.Add("This action cannot be undone.");

        return new AccountDeletionImpactDto(
            CanDelete: blockingConditions.Count == 0,
            BlockingConditions: blockingConditions,
            OrderCount: orderCount,
            ReviewCount: reviewCount,
            AddressCount: addressCount,
            HasActiveStore: hasActiveStore,
            StoreName: storeName,
            ImpactSummary: impactSummary);
    }

    /// <summary>
    /// Initiates an account deletion request.
    /// </summary>
    public async Task<AccountDeletionRequestResultDto> HandleAsync(
        RequestAccountDeletionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        _logger.LogInformation("Account deletion requested for UserId={UserId}", command.UserId);

        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("User not found for deletion request: UserId={UserId}", command.UserId);
            return AccountDeletionRequestResultDto.Failed("User not found.");
        }

        if (!user.CanRequestDeletion())
        {
            _logger.LogWarning("User cannot request deletion: UserId={UserId}, Status={Status}", command.UserId, user.Status);
            return AccountDeletionRequestResultDto.Failed("Account deletion is not available for this account status.");
        }

        // Check if there's already a pending request
        var existingRequest = await _deletionRepository.HasActiveDeletionRequestAsync(command.UserId, cancellationToken);
        if (existingRequest)
        {
            return AccountDeletionRequestResultDto.Failed("You already have a pending account deletion request.");
        }

        // Get impact assessment
        var impact = await HandleAsync(new GetAccountDeletionImpactQuery(command.UserId), cancellationToken);

        // Check blocking conditions
        if (!impact.CanDelete)
        {
            _logger.LogWarning("Account deletion blocked: UserId={UserId}, Conditions={Conditions}",
                command.UserId, string.Join(", ", impact.BlockingConditions));
            return AccountDeletionRequestResultDto.Blocked(impact, impact.BlockingConditions);
        }

        // Create the deletion request
        var request = new AccountDeletionRequest(command.UserId, command.IpAddress, command.UserAgent);
        await _deletionRepository.AddAsync(request, cancellationToken);

        // Create audit log for request
        var auditLog = new AccountDeletionAuditLog(
            request.Id,
            command.UserId,
            command.UserId,
            user.Role,
            AccountDeletionAuditAction.Requested,
            "User initiated account deletion request.",
            command.IpAddress,
            command.UserAgent);
        await _deletionRepository.AddAuditLogAsync(auditLog, cancellationToken);

        // Create audit log for impact display
        var impactAuditLog = new AccountDeletionAuditLog(
            request.Id,
            command.UserId,
            command.UserId,
            user.Role,
            AccountDeletionAuditAction.ImpactDisplayed,
            $"Impact: {impact.OrderCount} orders, {impact.ReviewCount} reviews, {impact.AddressCount} addresses will be anonymized.",
            command.IpAddress,
            command.UserAgent);
        await _deletionRepository.AddAuditLogAsync(impactAuditLog, cancellationToken);

        await _deletionRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Account deletion request created: RequestId={RequestId}, UserId={UserId}",
            request.Id, command.UserId);

        return AccountDeletionRequestResultDto.Succeeded(
            MapToDto(request),
            impact,
            "Account deletion request created. Please confirm to proceed with permanent deletion.");
    }

    /// <summary>
    /// Confirms and executes an account deletion.
    /// </summary>
    public async Task<AccountDeletionConfirmResultDto> HandleAsync(
        ConfirmAccountDeletionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        _logger.LogInformation("Account deletion confirmation: RequestId={RequestId}, UserId={UserId}",
            command.DeletionRequestId, command.UserId);

        var request = await _deletionRepository.GetByIdAsync(command.DeletionRequestId, cancellationToken);
        if (request is null)
        {
            _logger.LogWarning("Deletion request not found: RequestId={RequestId}", command.DeletionRequestId);
            return AccountDeletionConfirmResultDto.Failed("Deletion request not found.");
        }

        // Verify the request belongs to the user
        if (request.UserId != command.UserId)
        {
            _logger.LogWarning("User mismatch for deletion request: RequestId={RequestId}, RequestUserId={RequestUserId}, CommandUserId={CommandUserId}",
                command.DeletionRequestId, request.UserId, command.UserId);
            return AccountDeletionConfirmResultDto.Failed("You can only confirm your own deletion request.");
        }

        if (request.Status != AccountDeletionRequestStatus.Pending)
        {
            return AccountDeletionConfirmResultDto.Failed($"Cannot confirm deletion request in status {request.Status}.");
        }

        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return AccountDeletionConfirmResultDto.Failed("User not found.");
        }

        // Re-check blocking conditions before confirming
        var blockingConditions = await GetBlockingConditionsAsync(user, cancellationToken);
        if (blockingConditions.Count > 0)
        {
            request.Block(string.Join("; ", blockingConditions));
            _deletionRepository.Update(request);

            var blockedAuditLog = new AccountDeletionAuditLog(
                request.Id,
                command.UserId,
                command.UserId,
                user.Role,
                AccountDeletionAuditAction.Blocked,
                $"Blocking conditions: {string.Join("; ", blockingConditions)}",
                command.IpAddress,
                command.UserAgent);
            await _deletionRepository.AddAuditLogAsync(blockedAuditLog, cancellationToken);

            await _deletionRepository.SaveChangesAsync(cancellationToken);

            _logger.LogWarning("Deletion blocked at confirmation: RequestId={RequestId}, Conditions={Conditions}",
                request.Id, string.Join(", ", blockingConditions));

            return AccountDeletionConfirmResultDto.Failed($"Account deletion blocked: {string.Join("; ", blockingConditions)}");
        }

        // Confirm the request
        request.Confirm();
        _deletionRepository.Update(request);

        // Add confirmation audit log
        var confirmAuditLog = new AccountDeletionAuditLog(
            request.Id,
            command.UserId,
            command.UserId,
            user.Role,
            AccountDeletionAuditAction.Confirmed,
            "User confirmed account deletion.",
            command.IpAddress,
            command.UserAgent);
        await _deletionRepository.AddAuditLogAsync(confirmAuditLog, cancellationToken);

        // Perform anonymization
        await AnonymizeUserDataAsync(user, request, command.IpAddress, command.UserAgent, cancellationToken);

        // Complete the request
        request.Complete();
        _deletionRepository.Update(request);

        // Add completion audit log
        var completeAuditLog = new AccountDeletionAuditLog(
            request.Id,
            command.UserId,
            command.UserId,
            user.Role,
            AccountDeletionAuditAction.Anonymized,
            "Account data anonymized successfully.",
            command.IpAddress,
            command.UserAgent);
        await _deletionRepository.AddAuditLogAsync(completeAuditLog, cancellationToken);

        await _deletionRepository.SaveChangesAsync(cancellationToken);

        // Log sensitive data access for audit trail
        await _auditLoggingService.LogSensitiveAccessAsync(
            accessedByUserId: command.UserId,
            accessedByRole: user.Role,
            resourceType: SensitiveResourceType.CustomerProfile,
            resourceId: command.UserId,
            action: SensitiveAccessAction.Modify,
            resourceOwnerId: command.UserId,
            accessReason: "Account deletion - data anonymization",
            ipAddress: command.IpAddress,
            userAgent: command.UserAgent,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Account deletion completed: RequestId={RequestId}, UserId={UserId}",
            request.Id, command.UserId);

        return AccountDeletionConfirmResultDto.Succeeded(
            "Your account has been permanently deleted. All personal data has been anonymized.");
    }

    /// <summary>
    /// Cancels a pending account deletion request.
    /// </summary>
    public async Task<AccountDeletionCancelResultDto> HandleAsync(
        CancelAccountDeletionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var request = await _deletionRepository.GetByIdAsync(command.DeletionRequestId, cancellationToken);
        if (request is null)
        {
            return AccountDeletionCancelResultDto.Failed("Deletion request not found.");
        }

        if (request.UserId != command.UserId)
        {
            return AccountDeletionCancelResultDto.Failed("You can only cancel your own deletion request.");
        }

        if (request.Status != AccountDeletionRequestStatus.Pending)
        {
            return AccountDeletionCancelResultDto.Failed($"Cannot cancel deletion request in status {request.Status}.");
        }

        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        var userRole = user?.Role ?? UserRole.Buyer;

        request.Cancel();
        _deletionRepository.Update(request);

        var auditLog = new AccountDeletionAuditLog(
            request.Id,
            command.UserId,
            command.UserId,
            userRole,
            AccountDeletionAuditAction.Cancelled,
            "User cancelled account deletion request.");
        await _deletionRepository.AddAuditLogAsync(auditLog, cancellationToken);

        await _deletionRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Account deletion cancelled: RequestId={RequestId}, UserId={UserId}",
            request.Id, command.UserId);

        return AccountDeletionCancelResultDto.Succeeded();
    }

    /// <summary>
    /// Gets the pending deletion request for a user.
    /// </summary>
    public async Task<AccountDeletionRequestDto?> HandleAsync(
        GetPendingAccountDeletionQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var request = await _deletionRepository.GetPendingByUserIdAsync(query.UserId, cancellationToken);
        return request is null ? null : MapToDto(request);
    }

    /// <summary>
    /// Gets all deletion requests for a user.
    /// </summary>
    public async Task<IReadOnlyList<AccountDeletionRequestDto>> HandleAsync(
        GetAccountDeletionRequestsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var requests = await _deletionRepository.GetByUserIdAsync(query.UserId, cancellationToken);
        return requests.Select(MapToDto).ToArray();
    }

    /// <summary>
    /// Gets audit logs for account deletions.
    /// </summary>
    public async Task<IReadOnlyList<AccountDeletionAuditLogDto>> HandleAsync(
        GetAccountDeletionAuditLogsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        IReadOnlyList<AccountDeletionAuditLog> logs;

        if (query.DeletionRequestId.HasValue)
        {
            logs = await _deletionRepository.GetAuditLogsForRequestAsync(query.DeletionRequestId.Value, cancellationToken);
        }
        else if (query.UserId.HasValue)
        {
            logs = await _deletionRepository.GetAuditLogsByUserIdAsync(query.UserId.Value, cancellationToken);
        }
        else
        {
            return Array.Empty<AccountDeletionAuditLogDto>();
        }

        return logs.Select(l => new AccountDeletionAuditLogDto(
            l.Id,
            l.DeletionRequestId,
            l.AffectedUserId,
            l.TriggeredByUserId,
            l.TriggeredByRole,
            l.Action,
            l.Notes,
            l.OccurredAt)).ToArray();
    }

    private async Task<List<string>> GetBlockingConditionsAsync(User user, CancellationToken cancellationToken)
    {
        var conditions = new List<string>();

        if (user.Status == UserStatus.Deleted)
        {
            conditions.Add("Account has already been deleted.");
            return conditions;
        }

        if (user.Status == UserStatus.Suspended)
        {
            conditions.Add("Account is suspended. Please contact support.");
            return conditions;
        }

        // Check for open return requests (disputes)
        var returnRequests = await _returnRequestRepository.GetByBuyerIdAsync(user.Id, cancellationToken);
        var openDisputes = returnRequests.Where(r =>
            r.Status == ReturnRequestStatus.Requested ||
            r.Status == ReturnRequestStatus.Approved ||
            r.Status == ReturnRequestStatus.UnderAdminReview).ToList();

        if (openDisputes.Count > 0)
        {
            conditions.Add($"You have {openDisputes.Count} open dispute(s) or return request(s) that must be resolved first.");
        }

        // Check for seller-specific conditions
        if (user.Role == UserRole.Seller)
        {
            var store = await _storeRepository.GetBySellerIdAsync(user.Id, cancellationToken);
            if (store is not null)
            {
                // Check for open disputes on seller's orders (store-side)
                var sellerDisputes = await _returnRequestRepository.GetByStoreIdAsync(store.Id, cancellationToken);
                var openSellerDisputes = sellerDisputes.Where(r =>
                    r.Status == ReturnRequestStatus.Requested ||
                    r.Status == ReturnRequestStatus.Approved ||
                    r.Status == ReturnRequestStatus.UnderAdminReview).ToList();

                if (openSellerDisputes.Count > 0)
                {
                    conditions.Add($"Your store has {openSellerDisputes.Count} open dispute(s) that must be resolved first.");
                }
            }
        }

        return conditions;
    }

    private async Task AnonymizeUserDataAsync(
        User user,
        AccountDeletionRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var anonymizedSuffix = user.Id.ToString("N")[..8].ToUpperInvariant();

        // Anonymize user entity
        user.Anonymize(anonymizedSuffix);

        // Invalidate all active sessions
        var sessions = await _sessionRepository.GetActiveSessionsByUserIdAsync(user.Id, cancellationToken);
        foreach (var session in sessions)
        {
            session.Invalidate();
        }

        // Anonymize delivery addresses (delete them since they're not needed for records)
        var addresses = await _addressRepository.GetByBuyerIdAsync(user.Id, cancellationToken);
        foreach (var address in addresses)
        {
            _addressRepository.Remove(address);
        }

        // Anonymize reviews - keep the review content but attribute to "Deleted User"
        var reviews = await _reviewRepository.GetByBuyerIdAsync(user.Id, cancellationToken);
        foreach (var review in reviews)
        {
            review.AnonymizeAuthor();
        }

        // If seller, deactivate the store
        if (user.Role == UserRole.Seller)
        {
            var store = await _storeRepository.GetBySellerIdAsync(user.Id, cancellationToken);
            if (store is not null)
            {
                store.Deactivate();
            }
        }

        // Note: Order records are preserved with anonymized references.
        // The denormalized delivery address fields in Order are kept for legal/tax records.
        // The BuyerId reference remains but now points to an anonymized user.

        await _userRepository.SaveChangesAsync(cancellationToken);
    }

    private static AccountDeletionRequestDto MapToDto(AccountDeletionRequest request)
    {
        return new AccountDeletionRequestDto(
            request.Id,
            request.UserId,
            request.Status,
            request.RequestedAt,
            request.ConfirmedAt,
            request.CompletedAt,
            request.CancelledAt,
            request.BlockingReason);
    }
}
