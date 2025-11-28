using SD.Project.Domain.ValueObjects;

namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents an internal user (team member) associated with a store.
/// Internal users have specific roles that define their permissions within the seller panel.
/// </summary>
public class InternalUser
{
    /// <summary>
    /// Unique identifier for the internal user record.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The ID of the store this internal user belongs to.
    /// </summary>
    public Guid StoreId { get; private set; }

    /// <summary>
    /// The ID of the user account. Null if the invitation is still pending.
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    /// The email address of the internal user.
    /// </summary>
    public Email Email { get; private set; } = default!;

    /// <summary>
    /// The role assigned to this internal user within the store.
    /// </summary>
    public InternalUserRole Role { get; private set; }

    /// <summary>
    /// The current status of the internal user.
    /// </summary>
    public InternalUserStatus Status { get; private set; }

    /// <summary>
    /// The UTC timestamp when this internal user was created/invited.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// The UTC timestamp when this internal user last had their role or status updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// The UTC timestamp when the user accepted the invitation and became active.
    /// </summary>
    public DateTime? ActivatedAt { get; private set; }

    /// <summary>
    /// The UTC timestamp when the user was deactivated.
    /// </summary>
    public DateTime? DeactivatedAt { get; private set; }

    /// <summary>
    /// The ID of the user who invited this internal user.
    /// </summary>
    public Guid InvitedByUserId { get; private set; }

    private InternalUser()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new pending internal user invitation.
    /// </summary>
    /// <param name="storeId">The ID of the store.</param>
    /// <param name="email">The email address of the user being invited.</param>
    /// <param name="role">The role to assign to the user.</param>
    /// <param name="invitedByUserId">The ID of the user sending the invitation.</param>
    public InternalUser(Guid storeId, Email email, InternalUserRole role, Guid invitedByUserId)
    {
        if (storeId == Guid.Empty)
        {
            throw new ArgumentException("Store ID is required.", nameof(storeId));
        }

        if (invitedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Inviting user ID is required.", nameof(invitedByUserId));
        }

        Id = Guid.NewGuid();
        StoreId = storeId;
        UserId = null; // Will be set when invitation is accepted
        Email = email ?? throw new ArgumentNullException(nameof(email));
        Role = role;
        Status = InternalUserStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        ActivatedAt = null;
        DeactivatedAt = null;
        InvitedByUserId = invitedByUserId;
    }

    /// <summary>
    /// Creates an internal user record for an existing store owner.
    /// This is used when a store is created to automatically add the owner as an internal user.
    /// </summary>
    public static InternalUser CreateForStoreOwner(Guid storeId, Guid userId, Email email)
    {
        if (storeId == Guid.Empty)
        {
            throw new ArgumentException("Store ID is required.", nameof(storeId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        var now = DateTime.UtcNow;
        return new InternalUser
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            UserId = userId,
            Email = email ?? throw new ArgumentNullException(nameof(email)),
            Role = InternalUserRole.StoreOwner,
            Status = InternalUserStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            ActivatedAt = now,
            DeactivatedAt = null,
            InvitedByUserId = userId // Self-invited as store owner
        };
    }

    /// <summary>
    /// Activates the internal user after they accept the invitation.
    /// </summary>
    /// <param name="userId">The ID of the user account that accepted the invitation.</param>
    public void Activate(Guid userId)
    {
        if (Status != InternalUserStatus.Pending)
        {
            throw new InvalidOperationException("Only pending users can be activated.");
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        UserId = userId;
        Status = InternalUserStatus.Active;
        ActivatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the internal user, preventing them from accessing the seller panel.
    /// </summary>
    public void Deactivate()
    {
        if (Status == InternalUserStatus.Deactivated)
        {
            return; // Already deactivated, no-op
        }

        if (Role == InternalUserRole.StoreOwner)
        {
            throw new InvalidOperationException("Store owners cannot be deactivated. Transfer ownership first.");
        }

        Status = InternalUserStatus.Deactivated;
        DeactivatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reactivates a previously deactivated internal user.
    /// </summary>
    public void Reactivate()
    {
        if (Status != InternalUserStatus.Deactivated)
        {
            throw new InvalidOperationException("Only deactivated users can be reactivated.");
        }

        Status = InternalUserStatus.Active;
        DeactivatedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the role of the internal user.
    /// </summary>
    /// <param name="newRole">The new role to assign.</param>
    public void UpdateRole(InternalUserRole newRole)
    {
        if (Role == InternalUserRole.StoreOwner && newRole != InternalUserRole.StoreOwner)
        {
            throw new InvalidOperationException("Cannot demote a store owner. Transfer ownership first.");
        }

        Role = newRole;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Indicates whether this internal user can access the seller panel.
    /// </summary>
    public bool CanAccessSellerPanel => Status == InternalUserStatus.Active && UserId.HasValue;

    /// <summary>
    /// Indicates whether this is the store owner.
    /// </summary>
    public bool IsStoreOwner => Role == InternalUserRole.StoreOwner;
}
