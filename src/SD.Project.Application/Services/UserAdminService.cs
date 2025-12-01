using Microsoft.Extensions.Logging;
using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for admin user management operations.
/// </summary>
public sealed class UserAdminService
{
    private readonly IUserRepository _userRepository;
    private readonly ILoginEventRepository _loginEventRepository;
    private readonly IUserBlockInfoRepository _userBlockInfoRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IInternalUserRepository _internalUserRepository;
    private readonly ILogger<UserAdminService> _logger;

    public UserAdminService(
        IUserRepository userRepository,
        ILoginEventRepository loginEventRepository,
        IUserBlockInfoRepository userBlockInfoRepository,
        IStoreRepository storeRepository,
        IInternalUserRepository internalUserRepository,
        ILogger<UserAdminService> logger)
    {
        _userRepository = userRepository;
        _loginEventRepository = loginEventRepository;
        _userBlockInfoRepository = userBlockInfoRepository;
        _storeRepository = storeRepository;
        _internalUserRepository = internalUserRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets a paginated and filtered list of users for admin management.
    /// </summary>
    public async Task<PagedResultDto<UserSummaryDto>> HandleAsync(
        GetUsersForAdminQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        _logger.LogInformation(
            "Admin searching users: Role={Role}, Status={Status}, Search={Search}, Page={Page}",
            query.RoleFilter,
            query.StatusFilter,
            query.SearchTerm,
            pageNumber);

        var (users, totalCount) = await _userRepository.GetFilteredUsersAsync(
            query.RoleFilter,
            query.StatusFilter,
            query.SearchTerm,
            pageNumber,
            pageSize,
            cancellationToken);

        var userDtos = users.Select(u => new UserSummaryDto(
            u.Id,
            u.Email.Value,
            u.FirstName,
            u.LastName,
            u.Role,
            u.Status,
            u.CreatedAt,
            u.IsEmailVerified)).ToList();

        return PagedResultDto<UserSummaryDto>.Create(userDtos, pageNumber, pageSize, totalCount);
    }

    /// <summary>
    /// Gets detailed information about a specific user including recent login activity.
    /// </summary>
    public async Task<UserDetailDto?> HandleAsync(
        GetUserDetailQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        _logger.LogInformation("Admin viewing user details: UserId={UserId}", query.UserId);

        var user = await _userRepository.GetByIdAsync(query.UserId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("User not found: UserId={UserId}", query.UserId);
            return null;
        }

        // Get recent login activity (last 10 events)
        var loginEvents = await _loginEventRepository.GetRecentByUserIdAsync(
            query.UserId,
            10,
            cancellationToken);

        var loginEventDtos = loginEvents.Select(e => new LoginEventSummaryDto(
            e.OccurredAt,
            e.IsSuccess,
            e.EventType,
            e.IpAddress,
            e.Location,
            e.FailureReason)).ToList();

        // Get blocking information if user is blocked
        UserBlockInfoDto? blockInfoDto = null;
        if (user.Status == UserStatus.Blocked)
        {
            var blockInfo = await _userBlockInfoRepository.GetActiveByUserIdAsync(query.UserId, cancellationToken);
            if (blockInfo is not null)
            {
                var adminUser = await _internalUserRepository.GetByIdAsync(blockInfo.BlockedByAdminId, cancellationToken);
                var adminEmail = adminUser?.Email.Value ?? "Unknown";

                blockInfoDto = new UserBlockInfoDto(
                    blockInfo.BlockedByAdminId,
                    adminEmail,
                    blockInfo.BlockedAt,
                    blockInfo.Reason,
                    blockInfo.Notes);
            }
        }

        return new UserDetailDto(
            user.Id,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            user.Role,
            user.Status,
            user.CreatedAt,
            user.IsEmailVerified,
            user.EmailVerifiedAt,
            user.CompanyName,
            user.PhoneNumber,
            user.KycStatus,
            user.KycSubmittedAt,
            user.KycReviewedAt,
            user.TwoFactorEnabled,
            user.TwoFactorEnabledAt,
            loginEventDtos,
            blockInfoDto);
    }

    /// <summary>
    /// Blocks a user account.
    /// </summary>
    public async Task<BlockUserResultDto> HandleAsync(
        BlockUserCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        _logger.LogInformation(
            "Admin blocking user: UserId={UserId}, AdminId={AdminId}, Reason={Reason}",
            command.UserId,
            command.AdminId,
            command.Reason);

        // Verify admin exists
        var admin = await _internalUserRepository.GetByIdAsync(command.AdminId, cancellationToken);
        if (admin is null)
        {
            _logger.LogWarning("Admin not found: AdminId={AdminId}", command.AdminId);
            return BlockUserResultDto.Failed("Admin user not found.");
        }

        // Get user to block
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("User not found: UserId={UserId}", command.UserId);
            return BlockUserResultDto.Failed("User not found.");
        }

        // Check if already blocked
        if (user.Status == UserStatus.Blocked)
        {
            _logger.LogWarning("User already blocked: UserId={UserId}", command.UserId);
            return BlockUserResultDto.Failed("User is already blocked.");
        }

        // Block the user
        user.Block();

        // Create audit log entry
        var blockInfo = new UserBlockInfo(
            command.UserId,
            command.AdminId,
            command.Reason,
            command.Notes);
        await _userBlockInfoRepository.AddAsync(blockInfo, cancellationToken);

        // If user is a seller, suspend their store
        if (user.Role == UserRole.Seller)
        {
            var store = await _storeRepository.GetBySellerIdAsync(command.UserId, cancellationToken);
            if (store is not null)
            {
                store.Suspend();
                _logger.LogInformation("Suspended store for blocked seller: StoreId={StoreId}", store.Id);
            }
        }

        // Save all changes (user, block info, and store) in a single transaction
        // All repositories share the same DbContext, so SaveChangesAsync commits all pending changes
        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User blocked successfully: UserId={UserId}, Reason={Reason}",
            command.UserId,
            command.Reason);

        return BlockUserResultDto.Success();
    }

    /// <summary>
    /// Unblocks a user account.
    /// </summary>
    public async Task<BlockUserResultDto> HandleAsync(
        UnblockUserCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        _logger.LogInformation(
            "Admin unblocking user: UserId={UserId}, AdminId={AdminId}",
            command.UserId,
            command.AdminId);

        // Verify admin exists
        var admin = await _internalUserRepository.GetByIdAsync(command.AdminId, cancellationToken);
        if (admin is null)
        {
            _logger.LogWarning("Admin not found: AdminId={AdminId}", command.AdminId);
            return BlockUserResultDto.Failed("Admin user not found.");
        }

        // Get user to unblock
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("User not found: UserId={UserId}", command.UserId);
            return BlockUserResultDto.Failed("User not found.");
        }

        // Check if blocked
        if (user.Status != UserStatus.Blocked)
        {
            _logger.LogWarning("User is not blocked: UserId={UserId}", command.UserId);
            return BlockUserResultDto.Failed("User is not blocked.");
        }

        // Update the block info record
        var blockInfo = await _userBlockInfoRepository.GetActiveByUserIdAsync(command.UserId, cancellationToken);
        if (blockInfo is not null)
        {
            blockInfo.Unblock(command.AdminId);
        }

        // Unblock the user
        user.Unblock();

        // Save all changes (user and block info) in a single transaction
        // All repositories share the same DbContext, so SaveChangesAsync commits all pending changes
        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User unblocked successfully: UserId={UserId}", command.UserId);

        return BlockUserResultDto.Success();
    }
}
