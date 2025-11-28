using SD.Project.Domain.Entities;

namespace SD.Project.Application.DTOs;

/// <summary>
/// DTO representing an internal user within a store.
/// </summary>
public sealed record InternalUserDto(
    Guid Id,
    Guid StoreId,
    Guid? UserId,
    string Email,
    InternalUserRole Role,
    InternalUserStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ActivatedAt,
    DateTime? DeactivatedAt,
    Guid InvitedByUserId,
    bool CanAccessSellerPanel,
    bool IsStoreOwner);
