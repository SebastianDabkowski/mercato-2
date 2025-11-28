using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Interfaces;
using SD.Project.Domain.Entities;

namespace SD.Project.Filters;

/// <summary>
/// A filter attribute that enforces role-based access control on Razor Pages.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequireRoleAttribute : Attribute, IAsyncPageFilter
{
    private readonly UserRole[] _requiredRoles;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequireRoleAttribute"/> class.
    /// </summary>
    /// <param name="roles">The roles that are allowed to access the page.</param>
    public RequireRoleAttribute(params UserRole[] roles)
    {
        _requiredRoles = roles ?? throw new ArgumentNullException(nameof(roles));
    }

    public async Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
    {
        await Task.CompletedTask;
    }

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var logger = httpContext.RequestServices.GetRequiredService<ILogger<RequireRoleAttribute>>();
        var authorizationService = httpContext.RequestServices.GetRequiredService<IAuthorizationService>();

        // Check if user is authenticated
        if (httpContext.User?.Identity?.IsAuthenticated != true)
        {
            logger.LogWarning("Authorization failure: Unauthenticated user attempted to access {PagePath}",
                context.ActionDescriptor.RelativePath);

            context.Result = new RedirectToPageResult("/Login", new { returnUrl = httpContext.Request.Path });
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

        // Check authorization using the central service
        var result = authorizationService.Authorize(userRole, _requiredRoles);
        if (!result.IsAuthorized)
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            logger.LogWarning("Authorization failure: User {UserId} with role {UserRole} denied access to {PagePath}. Required roles: {RequiredRoles}",
                userId,
                userRole,
                context.ActionDescriptor.RelativePath,
                string.Join(", ", _requiredRoles));

            context.Result = CreateAccessDeniedResult(result.ErrorMessage ?? "Access denied.");
            return;
        }

        await next();
    }

    private static IActionResult CreateAccessDeniedResult(string message)
    {
        return new RedirectToPageResult("/Error", new { message });
    }
}
