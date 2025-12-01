using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SD.Project.Application.Interfaces;
using SD.Project.Domain.Entities;

namespace SD.Project.Filters;

/// <summary>
/// A filter attribute that enforces permission-based access control on Razor Pages.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequirePermissionAttribute : Attribute, IAsyncPageFilter
{
    private const string LoginPagePath = "/Login";
    private const string ErrorPagePath = "/Error";

    private readonly Permission[] _requiredPermissions;
    private readonly bool _requireAll;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequirePermissionAttribute"/> class.
    /// </summary>
    /// <param name="permissions">The permissions required to access the page.</param>
    public RequirePermissionAttribute(params Permission[] permissions)
        : this(false, permissions)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequirePermissionAttribute"/> class.
    /// </summary>
    /// <param name="requireAll">If true, all permissions are required; if false, any one permission is sufficient.</param>
    /// <param name="permissions">The permissions to check.</param>
    public RequirePermissionAttribute(bool requireAll, params Permission[] permissions)
    {
        _requiredPermissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _requireAll = requireAll;
    }

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
    {
        return Task.CompletedTask;
    }

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var logger = httpContext.RequestServices.GetRequiredService<ILogger<RequirePermissionAttribute>>();
        var authorizationService = httpContext.RequestServices.GetRequiredService<IAuthorizationService>();

        // Check if user is authenticated
        if (httpContext.User?.Identity?.IsAuthenticated != true)
        {
            logger.LogWarning("Authorization failure: Unauthenticated user attempted to access {PagePath}",
                context.ActionDescriptor.RelativePath);

            context.Result = new RedirectToPageResult(LoginPagePath, new { returnUrl = httpContext.Request.Path });
            return;
        }

        // Get the user's role from claims
        var roleClaim = httpContext.User.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(roleClaim) || !Enum.TryParse<UserRole>(roleClaim, out var userRole))
        {
            logger.LogWarning("Authorization failure: User has no valid role claim when accessing {PagePath}",
                context.ActionDescriptor.RelativePath);

            context.Result = CreateAccessDeniedResult("Access denied. Your account does not have a valid role.");
            return;
        }

        // Check permissions using the central authorization service
        bool hasPermission;
        if (_requireAll)
        {
            hasPermission = true;
            foreach (var permission in _requiredPermissions)
            {
                if (!await authorizationService.HasPermissionAsync(userRole, permission, httpContext.RequestAborted))
                {
                    hasPermission = false;
                    break;
                }
            }
        }
        else
        {
            hasPermission = await authorizationService.HasAnyPermissionAsync(
                userRole,
                _requiredPermissions,
                httpContext.RequestAborted);
        }

        if (!hasPermission)
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            logger.LogWarning(
                "Authorization failure: User {UserId} with role {UserRole} denied access to {PagePath}. Required permissions: {RequiredPermissions}",
                userId,
                userRole,
                context.ActionDescriptor.RelativePath,
                string.Join(", ", _requiredPermissions));

            context.Result = CreateAccessDeniedResult(
                $"Access denied. You do not have the required permission(s): {string.Join(", ", _requiredPermissions)}.");
            return;
        }

        await next();
    }

    private static IActionResult CreateAccessDeniedResult(string message)
    {
        return new RedirectToPageResult(ErrorPagePath, new { message });
    }
}
