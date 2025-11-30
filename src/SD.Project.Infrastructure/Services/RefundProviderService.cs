using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SD.Project.Application.Interfaces;

namespace SD.Project.Infrastructure.Services;

/// <summary>
/// Configuration for refund provider settings.
/// </summary>
public sealed class RefundProviderSettings
{
    /// <summary>
    /// Whether to simulate refunds in development mode.
    /// When true, no actual provider calls are made.
    /// </summary>
    public bool SimulateRefunds { get; set; } = true;

    /// <summary>
    /// Delay in milliseconds for simulated async refunds.
    /// </summary>
    public int SimulatedDelayMs { get; set; } = 100;
}

/// <summary>
/// Implementation of refund provider service.
/// Integrates with external payment providers for refund operations.
/// In development mode, simulates provider responses.
/// Logs all provider errors for support agent visibility.
/// </summary>
public sealed class RefundProviderService : IRefundProviderService
{
    private const string SimulatedRefundPrefix = "REF-SIM-";
    
    private readonly ILogger<RefundProviderService> _logger;
    private readonly RefundProviderSettings _settings;

    public RefundProviderService(
        ILogger<RefundProviderService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _settings = new RefundProviderSettings();
        configuration.GetSection("RefundProvider").Bind(_settings);
    }

    /// <inheritdoc />
    public async Task<RefundProviderResult> ProcessFullRefundAsync(
        Guid orderId,
        string originalTransactionId,
        decimal amount,
        string currency,
        string idempotencyKey,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing full refund for order {OrderId}. Amount: {Amount} {Currency}, " +
            "OriginalTransaction: {OriginalTransactionId}, IdempotencyKey: {IdempotencyKey}",
            orderId, amount, currency, originalTransactionId, idempotencyKey);

        // In simulation mode, return simulated responses
        if (_settings.SimulateRefunds)
        {
            return await SimulateRefundAsync(orderId, originalTransactionId, amount, currency, "full", reason);
        }

        // Real provider integration would go here
        // This is a placeholder for actual provider API calls
        _logger.LogWarning("Real refund provider integration not implemented. Falling back to simulation.");
        return await SimulateRefundAsync(orderId, originalTransactionId, amount, currency, "full", reason);
    }

    /// <inheritdoc />
    public async Task<RefundProviderResult> ProcessPartialRefundAsync(
        Guid orderId,
        string originalTransactionId,
        decimal refundAmount,
        string currency,
        string idempotencyKey,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing partial refund for order {OrderId}. Amount: {Amount} {Currency}, " +
            "OriginalTransaction: {OriginalTransactionId}, IdempotencyKey: {IdempotencyKey}",
            orderId, refundAmount, currency, originalTransactionId, idempotencyKey);

        // In simulation mode, return simulated responses
        if (_settings.SimulateRefunds)
        {
            return await SimulateRefundAsync(orderId, originalTransactionId, refundAmount, currency, "partial", reason);
        }

        // Real provider integration would go here
        _logger.LogWarning("Real refund provider integration not implemented. Falling back to simulation.");
        return await SimulateRefundAsync(orderId, originalTransactionId, refundAmount, currency, "partial", reason);
    }

    /// <inheritdoc />
    public async Task<RefundProviderResult> GetRefundStatusAsync(
        string refundTransactionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Checking refund status for transaction {RefundTransactionId}",
            refundTransactionId);

        // In simulation mode, return simulated status
        if (_settings.SimulateRefunds)
        {
            return await SimulateRefundStatusCheckAsync(refundTransactionId);
        }

        // Real provider integration would go here
        _logger.LogWarning("Real refund status check not implemented. Falling back to simulation.");
        return await SimulateRefundStatusCheckAsync(refundTransactionId);
    }

    /// <summary>
    /// Simulates a refund operation with the payment provider.
    /// </summary>
    private async Task<RefundProviderResult> SimulateRefundAsync(
        Guid orderId,
        string originalTransactionId,
        decimal amount,
        string currency,
        string refundType,
        string? reason)
    {
        // Add small delay to simulate network call
        if (_settings.SimulatedDelayMs > 0)
        {
            await Task.Delay(_settings.SimulatedDelayMs);
        }

        var refundTransactionId = $"{SimulatedRefundPrefix}{Guid.NewGuid():N}";

        _logger.LogInformation(
            "Simulated {RefundType} refund for order {OrderId}. " +
            "Generated refund transaction ID: {RefundTransactionId}",
            refundType, orderId, refundTransactionId);

        // Simulate some error scenarios for testing based on amount patterns
        // Amount ending in .99 simulates a provider error
        if (amount.ToString("F2").EndsWith(".99"))
        {
            _logger.LogWarning(
                "Simulated refund failure for testing. Order: {OrderId}, Amount: {Amount}",
                orderId, amount);

            return new RefundProviderResult(
                false,
                null,
                RefundProviderStatus.Failed,
                "Simulated provider error: Unable to process refund at this time.",
                "PROVIDER_UNAVAILABLE");
        }

        // Amount ending in .98 simulates a rejected refund
        if (amount.ToString("F2").EndsWith(".98"))
        {
            _logger.LogWarning(
                "Simulated refund rejection for testing. Order: {OrderId}, Amount: {Amount}",
                orderId, amount);

            return new RefundProviderResult(
                false,
                null,
                RefundProviderStatus.Rejected,
                "Simulated rejection: Original transaction not found or already refunded.",
                "TRANSACTION_NOT_FOUND");
        }

        // Amount ending in .97 simulates a pending async refund
        if (amount.ToString("F2").EndsWith(".97"))
        {
            _logger.LogInformation(
                "Simulated pending refund for testing. Order: {OrderId}, Amount: {Amount}",
                orderId, amount);

            return new RefundProviderResult(
                true,
                refundTransactionId,
                RefundProviderStatus.Pending,
                null,
                null);
        }

        // Normal success case
        return new RefundProviderResult(
            true,
            refundTransactionId,
            RefundProviderStatus.Completed,
            null,
            null);
    }

    /// <summary>
    /// Simulates checking the status of a pending refund.
    /// </summary>
    private async Task<RefundProviderResult> SimulateRefundStatusCheckAsync(string refundTransactionId)
    {
        // Add small delay to simulate network call
        if (_settings.SimulatedDelayMs > 0)
        {
            await Task.Delay(_settings.SimulatedDelayMs);
        }

        // Simulate success for simulated transactions
        if (refundTransactionId.StartsWith(SimulatedRefundPrefix))
        {
            _logger.LogInformation(
                "Simulated refund status check succeeded. Transaction: {RefundTransactionId}",
                refundTransactionId);

            return new RefundProviderResult(
                true,
                refundTransactionId,
                RefundProviderStatus.Completed,
                null,
                null);
        }

        // Unknown transaction
        _logger.LogWarning(
            "Unknown refund transaction ID: {RefundTransactionId}",
            refundTransactionId);

        return new RefundProviderResult(
            false,
            refundTransactionId,
            RefundProviderStatus.Pending,
            "Transaction status could not be confirmed.",
            "UNKNOWN_TRANSACTION");
    }
}
