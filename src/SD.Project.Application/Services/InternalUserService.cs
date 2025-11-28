using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Domain.ValueObjects;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service orchestrating internal user management use cases.
/// </summary>
public sealed class InternalUserService
{
    private readonly IInternalUserRepository _internalUserRepository;
    private readonly IInternalUserInvitationRepository _invitationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IUserSessionRepository _userSessionRepository;
    private readonly INotificationService _notificationService;

    public InternalUserService(
        IInternalUserRepository internalUserRepository,
        IInternalUserInvitationRepository invitationRepository,
        IUserRepository userRepository,
        IStoreRepository storeRepository,
        IUserSessionRepository userSessionRepository,
        INotificationService notificationService)
    {
        _internalUserRepository = internalUserRepository;
        _invitationRepository = invitationRepository;
        _userRepository = userRepository;
        _storeRepository = storeRepository;
        _userSessionRepository = userSessionRepository;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Gets all internal users for a store.
    /// </summary>
    public async Task<IReadOnlyList<InternalUserDto>> HandleAsync(GetInternalUsersQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var internalUsers = await _internalUserRepository.GetByStoreIdAsync(query.StoreId, cancellationToken);
        return internalUsers.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Gets a specific internal user by ID.
    /// </summary>
    public async Task<InternalUserDto?> HandleAsync(GetInternalUserByIdQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var internalUser = await _internalUserRepository.GetByIdAsync(query.InternalUserId, cancellationToken);
        return internalUser is null ? null : MapToDto(internalUser);
    }

    /// <summary>
    /// Invites a new internal user to a store.
    /// </summary>
    public async Task<InternalUserResultDto> HandleAsync(InviteInternalUserCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Verify the store exists
        var store = await _storeRepository.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null)
        {
            return InternalUserResultDto.Failed("Store not found.");
        }

        // Verify the inviting user has permission (must be store owner)
        var invitingUser = await _internalUserRepository.GetByStoreAndUserIdAsync(command.StoreId, command.InvitedByUserId, cancellationToken);
        if (invitingUser is null || invitingUser.Role != InternalUserRole.StoreOwner)
        {
            return InternalUserResultDto.Failed("Only store owners can invite internal users.");
        }

        // Validate email
        Email email;
        try
        {
            email = Email.Create(command.Email);
        }
        catch (ArgumentException ex)
        {
            return InternalUserResultDto.Failed(ex.Message);
        }

        // Check if email is already associated with this store
        if (await _internalUserRepository.ExistsByStoreAndEmailAsync(command.StoreId, email, cancellationToken))
        {
            return InternalUserResultDto.Failed("This email is already associated with your store.");
        }

        // Prevent inviting as StoreOwner (ownership transfer should be a separate operation)
        if (command.Role == InternalUserRole.StoreOwner)
        {
            return InternalUserResultDto.Failed("Cannot invite users as store owners. Use ownership transfer instead.");
        }

        try
        {
            // Create the internal user record
            var internalUser = new InternalUser(command.StoreId, email, command.Role, command.InvitedByUserId);
            await _internalUserRepository.AddAsync(internalUser, cancellationToken);

            // Create invitation token
            var invitation = new InternalUserInvitation(internalUser.Id);
            await _invitationRepository.AddAsync(invitation, cancellationToken);

            await _internalUserRepository.SaveChangesAsync(cancellationToken);

            // Send invitation email
            await _notificationService.SendInternalUserInvitationAsync(
                email.Value,
                store.Name,
                command.Role.ToString(),
                invitation.Token,
                cancellationToken);

            return InternalUserResultDto.Succeeded(internalUser.Id, "Invitation sent successfully.");
        }
        catch (ArgumentException ex)
        {
            return InternalUserResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Updates an internal user's role.
    /// </summary>
    public async Task<InternalUserResultDto> HandleAsync(UpdateInternalUserRoleCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var internalUser = await _internalUserRepository.GetByIdAsync(command.InternalUserId, cancellationToken);
        if (internalUser is null)
        {
            return InternalUserResultDto.Failed("Internal user not found.");
        }

        // Verify the requesting user has permission (must be store owner)
        var requestingUser = await _internalUserRepository.GetByStoreAndUserIdAsync(internalUser.StoreId, command.RequestedByUserId, cancellationToken);
        if (requestingUser is null || requestingUser.Role != InternalUserRole.StoreOwner)
        {
            return InternalUserResultDto.Failed("Only store owners can update user roles.");
        }

        // Prevent changing to StoreOwner (ownership transfer should be a separate operation)
        if (command.NewRole == InternalUserRole.StoreOwner)
        {
            return InternalUserResultDto.Failed("Cannot change role to store owner. Use ownership transfer instead.");
        }

        try
        {
            internalUser.UpdateRole(command.NewRole);
            await _internalUserRepository.SaveChangesAsync(cancellationToken);

            return InternalUserResultDto.Succeeded(internalUser.Id, "Role updated successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return InternalUserResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Deactivates an internal user.
    /// </summary>
    public async Task<InternalUserResultDto> HandleAsync(DeactivateInternalUserCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var internalUser = await _internalUserRepository.GetByIdAsync(command.InternalUserId, cancellationToken);
        if (internalUser is null)
        {
            return InternalUserResultDto.Failed("Internal user not found.");
        }

        // Verify the requesting user has permission (must be store owner)
        var requestingUser = await _internalUserRepository.GetByStoreAndUserIdAsync(internalUser.StoreId, command.RequestedByUserId, cancellationToken);
        if (requestingUser is null || requestingUser.Role != InternalUserRole.StoreOwner)
        {
            return InternalUserResultDto.Failed("Only store owners can deactivate internal users.");
        }

        // Prevent self-deactivation
        if (internalUser.Id == requestingUser.Id)
        {
            return InternalUserResultDto.Failed("You cannot deactivate your own account.");
        }

        try
        {
            internalUser.Deactivate();
            await _internalUserRepository.SaveChangesAsync(cancellationToken);

            // Invalidate all active sessions for the deactivated user
            if (internalUser.UserId.HasValue)
            {
                var sessions = await _userSessionRepository.GetActiveSessionsByUserIdAsync(internalUser.UserId.Value, cancellationToken);
                foreach (var session in sessions)
                {
                    session.Revoke();
                    await _userSessionRepository.UpdateAsync(session, cancellationToken);
                }
                if (sessions.Count > 0)
                {
                    await _userSessionRepository.SaveChangesAsync(cancellationToken);
                }
            }

            return InternalUserResultDto.Succeeded(internalUser.Id, "User deactivated successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return InternalUserResultDto.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Accepts an internal user invitation.
    /// </summary>
    public async Task<InternalUserResultDto> HandleAsync(AcceptInternalUserInvitationCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.InvitationToken))
        {
            return InternalUserResultDto.Failed("Invitation token is required.");
        }

        var invitation = await _invitationRepository.GetByTokenAsync(command.InvitationToken, cancellationToken);
        if (invitation is null)
        {
            return InternalUserResultDto.Failed("Invalid invitation token.");
        }

        if (!invitation.IsValid)
        {
            return invitation.IsExpired
                ? InternalUserResultDto.Failed("This invitation has expired. Please request a new invitation.")
                : InternalUserResultDto.Failed("This invitation has already been accepted.");
        }

        var internalUser = await _internalUserRepository.GetByIdAsync(invitation.InternalUserId, cancellationToken);
        if (internalUser is null)
        {
            return InternalUserResultDto.Failed("Internal user record not found.");
        }

        // Verify the accepting user exists
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return InternalUserResultDto.Failed("User not found.");
        }

        // Verify the email matches
        if (user.Email != internalUser.Email)
        {
            return InternalUserResultDto.Failed("This invitation was sent to a different email address.");
        }

        try
        {
            invitation.Accept();
            internalUser.Activate(command.UserId);

            await _internalUserRepository.SaveChangesAsync(cancellationToken);

            return InternalUserResultDto.Succeeded(internalUser.Id, "Invitation accepted successfully. You now have access to the seller panel.");
        }
        catch (InvalidOperationException ex)
        {
            return InternalUserResultDto.Failed(ex.Message);
        }
    }

    private static InternalUserDto MapToDto(InternalUser internalUser)
    {
        return new InternalUserDto(
            internalUser.Id,
            internalUser.StoreId,
            internalUser.UserId,
            internalUser.Email.Value,
            internalUser.Role,
            internalUser.Status,
            internalUser.CreatedAt,
            internalUser.UpdatedAt,
            internalUser.ActivatedAt,
            internalUser.DeactivatedAt,
            internalUser.InvitedByUserId,
            internalUser.CanAccessSellerPanel,
            internalUser.IsStoreOwner);
    }
}
