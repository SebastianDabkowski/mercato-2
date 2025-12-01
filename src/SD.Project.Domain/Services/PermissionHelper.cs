using SD.Project.Domain.Entities;

namespace SD.Project.Domain.Services;

/// <summary>
/// Helper methods for working with permissions and modules.
/// </summary>
public static class PermissionHelper
{
    /// <summary>
    /// Gets the module that a permission belongs to based on its numeric value.
    /// </summary>
    /// <param name="permission">The permission to get the module for.</param>
    /// <returns>The module the permission belongs to.</returns>
    public static PermissionModule GetModule(Permission permission)
    {
        var value = (int)permission;

        return value switch
        {
            >= 100 and < 200 => PermissionModule.Product,
            >= 200 and < 300 => PermissionModule.Order,
            >= 300 and < 400 => PermissionModule.Return,
            >= 400 and < 500 => PermissionModule.User,
            >= 500 and < 600 => PermissionModule.Store,
            >= 600 and < 700 => PermissionModule.Payment,
            >= 700 and < 800 => PermissionModule.Review,
            >= 800 and < 900 => PermissionModule.Report,
            >= 900 and < 1000 => PermissionModule.Category,
            >= 1000 and < 1100 => PermissionModule.Settings,
            >= 1100 and < 1200 => PermissionModule.Compliance,
            >= 1200 and < 1300 => PermissionModule.Rbac,
            _ => throw new ArgumentOutOfRangeException(nameof(permission), $"Unknown permission value: {value}")
        };
    }

    /// <summary>
    /// Gets all permissions that belong to a specific module.
    /// </summary>
    /// <param name="module">The module to get permissions for.</param>
    /// <returns>A collection of permissions belonging to the module.</returns>
    public static IReadOnlyCollection<Permission> GetPermissionsForModule(PermissionModule module)
    {
        return Enum.GetValues<Permission>()
            .Where(p => GetModule(p) == module)
            .ToArray();
    }

    /// <summary>
    /// Groups all permissions by their module.
    /// </summary>
    /// <returns>A dictionary of modules to their permissions.</returns>
    public static IReadOnlyDictionary<PermissionModule, IReadOnlyCollection<Permission>> GroupByModule()
    {
        return Enum.GetValues<Permission>()
            .GroupBy(GetModule)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyCollection<Permission>)g.ToArray());
    }
}
