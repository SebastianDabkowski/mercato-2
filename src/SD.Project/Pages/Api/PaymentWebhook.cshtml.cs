using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.Services;
using System.Text.Json;

namespace SD.Project.Pages.Api;

/// <summary>
/// API endpoint for receiving payment webhook notifications from payment providers.
/// This endpoint processes payment status updates (paid, failed, refunded) from external providers.
/// </summary>
[IgnoreAntiforgeryToken]
public class PaymentWebhookModel : PageModel
{
    private readonly PaymentWebhookService _paymentWebhookService;
    private readonly ILogger<PaymentWebhookModel> _logger;

    public PaymentWebhookModel(
        PaymentWebhookService paymentWebhookService,
        ILogger<PaymentWebhookModel> logger)
    {
        _paymentWebhookService = paymentWebhookService;
        _logger = logger;
    }

    /// <summary>
    /// Handles POST requests from payment providers.
    /// Expected JSON body structure:
    /// {
    ///     "orderId": "guid",
    ///     "transactionId": "string",
    ///     "status": "string",
    ///     "provider": "string" (optional),
    ///     "signature": "string" (optional)
    /// }
    /// </summary>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Read the raw request body
            using var reader = new StreamReader(Request.Body);
            var rawPayload = await reader.ReadToEndAsync(cancellationToken);

            _logger.LogInformation("Received payment webhook: {Payload}", rawPayload);

            // Parse the webhook payload
            PaymentWebhookPayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<PaymentWebhookPayload>(rawPayload, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse payment webhook payload");
                return BadRequest(new { error = "Invalid JSON payload" });
            }

            if (payload is null)
            {
                return BadRequest(new { error = "Empty payload" });
            }

            // Validate required fields
            if (payload.OrderId == Guid.Empty)
            {
                return BadRequest(new { error = "Missing orderId" });
            }

            if (string.IsNullOrWhiteSpace(payload.TransactionId))
            {
                return BadRequest(new { error = "Missing transactionId" });
            }

            if (string.IsNullOrWhiteSpace(payload.Status))
            {
                return BadRequest(new { error = "Missing status" });
            }

            // Get provider from header or payload
            var providerName = Request.Headers["X-Payment-Provider"].FirstOrDefault()
                ?? payload.Provider;

            // Get signature from header for verification
            var signature = Request.Headers["X-Webhook-Signature"].FirstOrDefault()
                ?? payload.Signature;

            // Process the webhook
            var command = new ProcessPaymentWebhookCommand(
                payload.OrderId,
                payload.TransactionId,
                payload.Status,
                providerName,
                signature,
                rawPayload,
                payload.RefundAmount);

            var result = await _paymentWebhookService.HandleAsync(command, cancellationToken);

            if (result.Success)
            {
                return new JsonResult(new
                {
                    success = true,
                    previousStatus = result.PreviousPaymentStatus,
                    newStatus = result.NewPaymentStatus,
                    message = result.Message
                });
            }
            else
            {
                // Return 200 for business logic failures (e.g., order not found, invalid transition)
                // to acknowledge receipt and prevent payment providers from retrying.
                // The actual error is logged for debugging.
                _logger.LogWarning("Payment webhook processing failed: {Message}", result.Message);
                return new JsonResult(new
                {
                    success = false,
                    message = result.Message
                });
            }
        }
        catch (Exception ex)
        {
            // Return 500 for unexpected server errors to allow payment providers to retry
            _logger.LogError(ex, "Unexpected error processing payment webhook");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Handles GET requests - returns webhook endpoint status.
    /// </summary>
    public IActionResult OnGet()
    {
        return new JsonResult(new { status = "ok", endpoint = "payment-webhook" });
    }

    /// <summary>
    /// Payload structure for payment webhooks.
    /// </summary>
    private sealed class PaymentWebhookPayload
    {
        public Guid OrderId { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Provider { get; set; }
        public string? Signature { get; set; }
        public decimal? RefundAmount { get; set; }
    }
}
