using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;

namespace SD.Project.Pages.Api;

/// <summary>
/// API endpoint for push notification subscription management.
/// </summary>
public class PushSubscriptionModel : PageModel
{
    private readonly PushSubscriptionService _pushSubscriptionService;
    private readonly ILogger<PushSubscriptionModel> _logger;

    public PushSubscriptionModel(
        PushSubscriptionService pushSubscriptionService,
        ILogger<PushSubscriptionModel> logger)
    {
        _pushSubscriptionService = pushSubscriptionService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the VAPID public key for client-side subscription.
    /// </summary>
    public IActionResult OnGetVapidKey()
    {
        var result = _pushSubscriptionService.HandleAsync(new GetVapidPublicKeyQuery());
        return new JsonResult(new { publicKey = result.PublicKey });
    }

    /// <summary>
    /// Gets all push subscriptions for the current user.
    /// </summary>
    public async Task<IActionResult> OnGetSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return new UnauthorizedResult();
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return new UnauthorizedResult();
        }

        var subscriptions = await _pushSubscriptionService.HandleAsync(
            new GetPushSubscriptionsQuery(userId),
            cancellationToken);

        return new JsonResult(new { subscriptions });
    }

    /// <summary>
    /// Checks if the user has any active push subscriptions.
    /// </summary>
    public async Task<IActionResult> OnGetStatusAsync(CancellationToken cancellationToken = default)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return new JsonResult(new { enabled = false, authenticated = false });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return new JsonResult(new { enabled = false, authenticated = false });
        }

        var hasSubscription = await _pushSubscriptionService.HandleAsync(
            new HasPushSubscriptionQuery(userId),
            cancellationToken);

        return new JsonResult(new { enabled = hasSubscription, authenticated = true });
    }

    /// <summary>
    /// Subscribes the current device to push notifications.
    /// </summary>
    public async Task<IActionResult> OnPostSubscribeAsync(
        [FromBody] SubscribeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return new UnauthorizedResult();
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return new UnauthorizedResult();
        }

        if (string.IsNullOrWhiteSpace(request.Endpoint) ||
            string.IsNullOrWhiteSpace(request.P256dh) ||
            string.IsNullOrWhiteSpace(request.Auth))
        {
            return new BadRequestObjectResult(new { success = false, error = "Invalid subscription data." });
        }

        _logger.LogInformation("User {UserId} subscribing to push notifications", userId);

        var command = new SubscribeToPushCommand(
            userId,
            request.Endpoint,
            request.P256dh,
            request.Auth,
            request.DeviceName);

        var result = await _pushSubscriptionService.HandleAsync(command, cancellationToken);

        if (result.Success)
        {
            _logger.LogInformation(
                "User {UserId} subscribed to push notifications. Subscription ID: {SubscriptionId}",
                userId, result.SubscriptionId);
        }

        return new JsonResult(new
        {
            success = result.Success,
            subscriptionId = result.SubscriptionId,
            error = result.ErrorMessage
        });
    }

    /// <summary>
    /// Unsubscribes the current device from push notifications.
    /// </summary>
    public async Task<IActionResult> OnPostUnsubscribeAsync(
        [FromBody] UnsubscribeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return new UnauthorizedResult();
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return new UnauthorizedResult();
        }

        if (string.IsNullOrWhiteSpace(request.Endpoint))
        {
            return new BadRequestObjectResult(new { success = false, error = "Endpoint is required." });
        }

        _logger.LogInformation("User {UserId} unsubscribing from push notifications", userId);

        var command = new UnsubscribeFromPushCommand(userId, request.Endpoint);
        var result = await _pushSubscriptionService.HandleAsync(command, cancellationToken);

        return new JsonResult(new
        {
            success = result.Success,
            error = result.ErrorMessage
        });
    }

    /// <summary>
    /// Toggles a specific push subscription on/off.
    /// </summary>
    public async Task<IActionResult> OnPostToggleAsync(
        [FromBody] ToggleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return new UnauthorizedResult();
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return new UnauthorizedResult();
        }

        var command = new TogglePushSubscriptionCommand(userId, request.SubscriptionId, request.Enable);
        var result = await _pushSubscriptionService.HandleAsync(command, cancellationToken);

        return new JsonResult(new
        {
            success = result.Success,
            error = result.ErrorMessage
        });
    }

    /// <summary>
    /// Deletes a specific push subscription.
    /// </summary>
    public async Task<IActionResult> OnPostDeleteAsync(
        [FromBody] DeleteRequest request,
        CancellationToken cancellationToken = default)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return new UnauthorizedResult();
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return new UnauthorizedResult();
        }

        var command = new DeletePushSubscriptionCommand(userId, request.SubscriptionId);
        var result = await _pushSubscriptionService.HandleAsync(command, cancellationToken);

        return new JsonResult(new
        {
            success = result.Success,
            error = result.ErrorMessage
        });
    }

    public record SubscribeRequest(
        string Endpoint,
        string P256dh,
        string Auth,
        string? DeviceName);

    public record UnsubscribeRequest(string Endpoint);

    public record ToggleRequest(Guid SubscriptionId, bool Enable);

    public record DeleteRequest(Guid SubscriptionId);
}
