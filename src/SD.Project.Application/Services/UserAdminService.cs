using Microsoft.Extensions.Logging;
using SD.Project.Application.DTOs;
using SD.Project.Application.Queries;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for admin user management operations.
/// </summary>
public sealed class UserAdminService
{
    private readonly IUserRepository _userRepository;
    private readonly ILoginEventRepository _loginEventRepository;
    private readonly ILogger<UserAdminService> _logger;

    public UserAdminService(
        IUserRepository userRepository,
        ILoginEventRepository loginEventRepository,
        ILogger<UserAdminService> logger)
    {
        _userRepository = userRepository;
        _loginEventRepository = loginEventRepository;
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
            loginEventDtos);
    }
}
