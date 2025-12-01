using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SD.Project.Application.Interfaces;
using SD.Project.Domain.Entities;

namespace SD.Project.Filters;

/// <summary>
/// The type of resource being accessed.
/// </summary>
public enum ResourceType
{
    /// <summary>Access is based on store ownership (seller accessing their products/orders).</summary>
    Store,
    /// <summary>Access is based on order ownership (buyer accessing their orders).</summary>
    BuyerOrder,
    /// <summary>Access is based on shipment ownership (seller accessing their shipments).</summary>
    SellerShipment,
    /// <summary>Access is based on product ownership (seller accessing their products).</summary>
    Product
}

/// <summary>
/// A filter attribute that enforces resource-level access control on Razor Pages.
/// Validates that the current user has permission to access the requested resource.
/// Admin users bypass resource ownership checks.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequireResourceOwnerAttribute : Attribute, IAsyncPageFilter
{
    private const string LoginPagePath = "/Login";
    private const string ErrorPagePath = "/Error";

    private readonly ResourceType _resourceType;
    private readonly string _resourceIdParameterName;
    private readonly bool _requireResourceId;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequireResourceOwnerAttribute"/> class.
    /// </summary>
    /// <param name="resourceType">The type of resource being accessed.</param>
    /// <param name="resourceIdParameterName">The name of the route/query parameter containing the resource ID. Defaults to "id".</param>
    /// <param name="requireResourceId">If true, denies access when resource ID is missing. Defaults to false to allow optional resource IDs for pages that may serve both detail and list views.</param>
    public RequireResourceOwnerAttribute(
        ResourceType resourceType,
        string resourceIdParameterName = "id",
        bool requireResourceId = false)
    {
        _resourceType = resourceType;
        _resourceIdParameterName = resourceIdParameterName;
        _requireResourceId = requireResourceId;
    }

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
    {
        return Task.CompletedTask;
    }

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var logger = httpContext.RequestServices.GetRequiredService<ILogger<RequireResourceOwnerAttribute>>();
        var authorizationService = httpContext.RequestServices.GetRequiredService<IAuthorizationService>();

        // Check if user is authenticated
        if (httpContext.User?.Identity?.IsAuthenticated != true)
        {
            logger.LogWarning("Authorization failure: Unauthenticated user attempted to access {PagePath}",
                context.ActionDescriptor.RelativePath);

            context.Result = new RedirectToPageResult(LoginPagePath, new { returnUrl = httpContext.Request.Path });
            return;
        }

        // Get user ID
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            logger.LogWarning("Authorization failure: User has no valid ID claim when accessing {PagePath}",
                context.ActionDescriptor.RelativePath);

            context.Result = CreateAccessDeniedResult("Access denied. Invalid user identification.");
            return;
        }

        // Get user role
        var roleClaim = httpContext.User.FindFirst(ClaimTypes.Role)?.Value;
        if (!Enum.TryParse<UserRole>(roleClaim, out var userRole))
        {
            logger.LogWarning("Authorization failure: User has no valid role claim when accessing {PagePath}",
                context.ActionDescriptor.RelativePath);

            context.Result = CreateAccessDeniedResult("Access denied. Invalid user role.");
            return;
        }

        // Admin users bypass resource ownership checks
        if (userRole == UserRole.Admin)
        {
            await next();
            return;
        }

        // Get resource ID from route data or query string
        if (!TryGetResourceId(context, out var resourceId))
        {
            // If resource ID is required but not present, deny access
            if (_requireResourceId)
            {
                logger.LogWarning(
                    "Authorization failure: User {UserId} attempted to access {ResourceType} without providing a resource ID on {PagePath}",
                    userId,
                    _resourceType,
                    context.ActionDescriptor.RelativePath);

                context.Result = CreateAccessDeniedResult("Access denied. Resource identifier is required.");
                return;
            }

            // If resource ID is optional (e.g., list pages), allow the request
            await next();
            return;
        }

        // Perform resource authorization check
        var result = await PerformResourceAuthorizationAsync(
            authorizationService,
            userId,
            resourceId,
            httpContext.RequestAborted);

        if (!result.IsAuthorized)
        {
            logger.LogWarning(
                "Authorization failure: User {UserId} with role {UserRole} denied access to {ResourceType} {ResourceId} on {PagePath}",
                userId,
                userRole,
                _resourceType,
                resourceId,
                context.ActionDescriptor.RelativePath);

            context.Result = CreateAccessDeniedResult(result.ErrorMessage ?? "Access denied.");
            return;
        }

        await next();
    }

    private bool TryGetResourceId(PageHandlerExecutingContext context, out Guid resourceId)
    {
        resourceId = Guid.Empty;

        // Try route values first
        if (context.RouteData.Values.TryGetValue(_resourceIdParameterName, out var routeValue) &&
            routeValue is string routeString &&
            Guid.TryParse(routeString, out resourceId))
        {
            return true;
        }

        // Try query string
        if (context.HttpContext.Request.Query.TryGetValue(_resourceIdParameterName, out var queryValue) &&
            Guid.TryParse(queryValue.ToString(), out resourceId))
        {
            return true;
        }

        // Try page handler arguments (for property-bound parameters)
        foreach (var argument in context.HandlerArguments)
        {
            if (argument.Key.Equals(_resourceIdParameterName, StringComparison.OrdinalIgnoreCase) &&
                argument.Value is Guid guidValue)
            {
                resourceId = guidValue;
                return true;
            }
        }

        return false;
    }

    private async Task<AuthorizationResult> PerformResourceAuthorizationAsync(
        IAuthorizationService authorizationService,
        Guid userId,
        Guid resourceId,
        CancellationToken cancellationToken)
    {
        return _resourceType switch
        {
            ResourceType.Store => await authorizationService.AuthorizeStoreAccessAsync(userId, resourceId, cancellationToken),
            ResourceType.BuyerOrder => await authorizationService.AuthorizeBuyerOrderAccessAsync(userId, resourceId, cancellationToken),
            ResourceType.SellerShipment => await authorizationService.AuthorizeSellerShipmentAccessAsync(userId, resourceId, cancellationToken),
            ResourceType.Product => await authorizationService.AuthorizeProductAccessAsync(userId, resourceId, cancellationToken),
            _ => AuthorizationResult.Failure("Unknown resource type.")
        };
    }

    private static IActionResult CreateAccessDeniedResult(string message)
    {
        // Use a generic message to avoid exposing sensitive authorization details in the URL
        // The detailed error is already logged for administrators to investigate
        return new RedirectToPageResult(ErrorPagePath, new { message = "Access denied. You do not have permission to access this resource." });
    }
}
