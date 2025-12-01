namespace SD.Project.Domain.Entities;

/// <summary>
/// Represents a permission assignment to a role.
/// This entity enables dynamic role-based access control where permissions
/// can be assigned or revoked from roles at runtime.
/// </summary>
public class RolePermission
{
    /// <summary>
    /// Unique identifier for this role-permission assignment.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The role that has this permission.
    /// </summary>
    public UserRole Role { get; private set; }

    /// <summary>
    /// The permission granted to the role.
    /// </summary>
    public Permission Permission { get; private set; }

    /// <summary>
    /// The UTC timestamp when this permission was assigned.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// The ID of the user who assigned this permission.
    /// </summary>
    public Guid CreatedByUserId { get; private set; }

    /// <summary>
    /// Indicates whether this permission assignment is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// The UTC timestamp when this permission was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// The ID of the user who last updated this permission.
    /// </summary>
    public Guid? UpdatedByUserId { get; private set; }

    private RolePermission()
    {
        // EF Core constructor
    }

    /// <summary>
    /// Creates a new role permission assignment.
    /// </summary>
    /// <param name="role">The role to assign the permission to.</param>
    /// <param name="permission">The permission to assign.</param>
    /// <param name="createdByUserId">The ID of the user creating this assignment.</param>
    public RolePermission(UserRole role, Permission permission, Guid createdByUserId)
    {
        if (createdByUserId == Guid.Empty)
        {
            throw new ArgumentException("Creator user ID is required.", nameof(createdByUserId));
        }

        Id = Guid.NewGuid();
        Role = role;
        Permission = permission;
        CreatedAt = DateTime.UtcNow;
        CreatedByUserId = createdByUserId;
        IsActive = true;
        UpdatedAt = null;
        UpdatedByUserId = null;
    }

    /// <summary>
    /// Activates this permission assignment.
    /// </summary>
    /// <param name="updatedByUserId">The ID of the user making this change.</param>
    public void Activate(Guid updatedByUserId)
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = updatedByUserId;
    }

    /// <summary>
    /// Deactivates this permission assignment.
    /// </summary>
    /// <param name="updatedByUserId">The ID of the user making this change.</param>
    public void Deactivate(Guid updatedByUserId)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = updatedByUserId;
    }
}
