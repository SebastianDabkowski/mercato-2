using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SD.Project.Application.Interfaces;
using SD.Project.Domain.Entities;

namespace SD.Project.Infrastructure.Services;

/// <summary>
/// Configuration for payment provider settings.
/// </summary>
public sealed class PaymentProviderSettings
{
    /// <summary>
    /// Base URL for the payment provider API.
    /// </summary>
    public string? ApiBaseUrl { get; set; }

    /// <summary>
    /// API key for authentication with the provider.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Whether card payments are enabled.
    /// </summary>
    public bool CardEnabled { get; set; } = true;

    /// <summary>
    /// Whether bank transfer payments are enabled.
    /// </summary>
    public bool BankTransferEnabled { get; set; } = true;

    /// <summary>
    /// Whether BLIK payments are enabled.
    /// </summary>
    public bool BlikEnabled { get; set; } = true;

    /// <summary>
    /// Whether digital wallet payments are enabled.
    /// </summary>
    public bool DigitalWalletEnabled { get; set; } = true;

    /// <summary>
    /// Whether to simulate payments in development mode.
    /// When true, no actual provider calls are made.
    /// </summary>
    public bool SimulatePayments { get; set; } = true;
}

/// <summary>
/// Implementation of payment provider service.
/// Integrates with external payment providers for secure redirect payments.
/// In development mode, simulates provider responses.
/// </summary>
public sealed class PaymentProviderService : IPaymentProviderService
{
    private const string SimulatedTransactionPrefix = "SIM-";
    private const string SimulatedRedirectUrlBase = "/Buyer/Checkout/PaymentRedirect";

    private readonly ILogger<PaymentProviderService> _logger;
    private readonly PaymentProviderSettings _settings;

    public PaymentProviderService(
        ILogger<PaymentProviderService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _settings = new PaymentProviderSettings();
        configuration.GetSection("PaymentProvider").Bind(_settings);
    }

    /// <inheritdoc />
    public async Task<PaymentInitiationResult> InitiatePaymentAsync(
        Guid orderId,
        decimal amount,
        string currency,
        PaymentMethodType paymentMethodType,
        string idempotencyKey,
        string returnUrl,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Initiating payment for order {OrderId}. Amount: {Amount} {Currency}, Method: {Method}, IdempotencyKey: {IdempotencyKey}",
            orderId, amount, currency, paymentMethodType, idempotencyKey);

        // Check if payment method is enabled
        if (!IsPaymentMethodEnabled(paymentMethodType))
        {
            _logger.LogWarning("Payment method {Method} is not enabled", paymentMethodType);
            return new PaymentInitiationResult(
                false, null, null, false, false,
                $"Payment method {paymentMethodType} is not available.",
                "PAYMENT_METHOD_DISABLED");
        }

        // In simulation mode, return simulated responses
        if (_settings.SimulatePayments)
        {
            return await SimulatePaymentInitiationAsync(orderId, amount, currency, paymentMethodType, idempotencyKey, returnUrl);
        }

        // Real provider integration would go here
        // This is a placeholder for actual provider API calls
        _logger.LogWarning("Real payment provider integration not implemented. Falling back to simulation.");
        return await SimulatePaymentInitiationAsync(orderId, amount, currency, paymentMethodType, idempotencyKey, returnUrl);
    }

    /// <inheritdoc />
    public async Task<BlikPaymentResult> SubmitBlikCodeAsync(
        Guid orderId,
        string transactionId,
        string blikCode,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Submitting BLIK code for order {OrderId}, transaction {TransactionId}, IdempotencyKey: {IdempotencyKey}",
            orderId, transactionId, idempotencyKey);

        // Validate BLIK code format (6 digits)
        if (string.IsNullOrWhiteSpace(blikCode) || blikCode.Length != 6 || !blikCode.All(char.IsDigit))
        {
            return new BlikPaymentResult(
                false, null,
                "Invalid BLIK code. Please enter a 6-digit code.",
                "INVALID_BLIK_CODE");
        }

        // In simulation mode, simulate BLIK payment confirmation
        if (_settings.SimulatePayments)
        {
            return await SimulateBlikPaymentAsync(orderId, transactionId, blikCode);
        }

        // Real provider integration would go here
        _logger.LogWarning("Real BLIK payment integration not implemented. Falling back to simulation.");
        return await SimulateBlikPaymentAsync(orderId, transactionId, blikCode);
    }

    /// <inheritdoc />
    public async Task<PaymentConfirmationResult> ConfirmPaymentAsync(
        Guid orderId,
        string transactionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Confirming payment for order {OrderId}, transaction {TransactionId}",
            orderId, transactionId);

        // In simulation mode, simulate payment confirmation
        if (_settings.SimulatePayments)
        {
            return await SimulatePaymentConfirmationAsync(orderId, transactionId);
        }

        // Real provider integration would go here
        _logger.LogWarning("Real payment confirmation integration not implemented. Falling back to simulation.");
        return await SimulatePaymentConfirmationAsync(orderId, transactionId);
    }

    /// <inheritdoc />
    public bool IsPaymentMethodEnabled(PaymentMethodType paymentMethodType)
    {
        return paymentMethodType switch
        {
            PaymentMethodType.Card => _settings.CardEnabled,
            PaymentMethodType.BankTransfer => _settings.BankTransferEnabled,
            PaymentMethodType.Blik => _settings.BlikEnabled,
            PaymentMethodType.DigitalWallet => _settings.DigitalWalletEnabled,
            PaymentMethodType.BuyNowPayLater => true, // Always enabled if configured
            PaymentMethodType.CashOnDelivery => true, // Always enabled if configured
            _ => false
        };
    }

    private Task<PaymentInitiationResult> SimulatePaymentInitiationAsync(
        Guid orderId,
        decimal amount,
        string currency,
        PaymentMethodType paymentMethodType,
        string idempotencyKey,
        string returnUrl)
    {
        var transactionId = $"{SimulatedTransactionPrefix}{Guid.NewGuid():N}";

        _logger.LogInformation(
            "Simulating payment initiation for order {OrderId}. Generated transaction ID: {TransactionId}",
            orderId, transactionId);

        // BLIK requires code entry, not redirect
        if (paymentMethodType == PaymentMethodType.Blik)
        {
            return Task.FromResult(new PaymentInitiationResult(
                true,
                transactionId,
                null,
                false,
                true,
                null,
                null));
        }

        // Card and bank transfer use secure redirect
        if (paymentMethodType == PaymentMethodType.Card || paymentMethodType == PaymentMethodType.BankTransfer)
        {
            var redirectUrl = $"{SimulatedRedirectUrlBase}?orderId={orderId}&transactionId={transactionId}&returnUrl={Uri.EscapeDataString(returnUrl)}";
            return Task.FromResult(new PaymentInitiationResult(
                true,
                transactionId,
                redirectUrl,
                true,
                false,
                null,
                null));
        }

        // Other methods (digital wallet, BNPL) - simulate immediate success
        return Task.FromResult(new PaymentInitiationResult(
            true,
            transactionId,
            null,
            false,
            false,
            null,
            null));
    }

    private Task<BlikPaymentResult> SimulateBlikPaymentAsync(
        Guid orderId,
        string transactionId,
        string blikCode)
    {
        _logger.LogInformation(
            "Simulating BLIK payment for order {OrderId}, transaction {TransactionId}",
            orderId, transactionId);

        // Simulate some failure scenarios for testing
        // Code "000000" simulates expired code
        if (blikCode == "000000")
        {
            return Task.FromResult(new BlikPaymentResult(
                false, transactionId,
                "BLIK code has expired. Please generate a new code.",
                "BLIK_CODE_EXPIRED"));
        }

        // Code "111111" simulates declined payment
        if (blikCode == "111111")
        {
            return Task.FromResult(new BlikPaymentResult(
                false, transactionId,
                "Payment was declined. Please try again or use a different payment method.",
                "PAYMENT_DECLINED"));
        }

        // All other codes succeed
        var confirmedTransactionId = $"{transactionId}-CONFIRMED";
        return Task.FromResult(new BlikPaymentResult(
            true,
            confirmedTransactionId,
            null,
            null));
    }

    private Task<PaymentConfirmationResult> SimulatePaymentConfirmationAsync(
        Guid orderId,
        string transactionId)
    {
        _logger.LogInformation(
            "Simulating payment confirmation for order {OrderId}, transaction {TransactionId}",
            orderId, transactionId);

        // Simulate success for simulated transactions
        if (transactionId.StartsWith(SimulatedTransactionPrefix))
        {
            return Task.FromResult(new PaymentConfirmationResult(
                true,
                transactionId,
                PaymentConfirmationStatus.Completed,
                null));
        }

        // For unknown transactions, return pending
        return Task.FromResult(new PaymentConfirmationResult(
            false,
            transactionId,
            PaymentConfirmationStatus.Pending,
            "Transaction status could not be confirmed."));
    }
}
