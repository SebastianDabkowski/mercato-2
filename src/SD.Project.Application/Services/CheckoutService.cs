using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;
using SD.Project.Domain.Services;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service orchestrating checkout use cases.
/// Handles shipping selection, payment, and order creation.
/// </summary>
public sealed class CheckoutService
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IShippingMethodRepository _shippingMethodRepository;
    private readonly IPaymentMethodRepository _paymentMethodRepository;
    private readonly IDeliveryAddressRepository _deliveryAddressRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    private readonly IPaymentProviderService _paymentProviderService;
    private readonly CheckoutValidationService _checkoutValidationService;
    private readonly EscrowService _escrowService;

    public CheckoutService(
        ICartRepository cartRepository,
        IProductRepository productRepository,
        IStoreRepository storeRepository,
        IShippingMethodRepository shippingMethodRepository,
        IPaymentMethodRepository paymentMethodRepository,
        IDeliveryAddressRepository deliveryAddressRepository,
        IOrderRepository orderRepository,
        IUserRepository userRepository,
        INotificationService notificationService,
        IPaymentProviderService paymentProviderService,
        CheckoutValidationService checkoutValidationService,
        EscrowService escrowService)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _storeRepository = storeRepository;
        _shippingMethodRepository = shippingMethodRepository;
        _paymentMethodRepository = paymentMethodRepository;
        _deliveryAddressRepository = deliveryAddressRepository;
        _orderRepository = orderRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
        _paymentProviderService = paymentProviderService;
        _checkoutValidationService = checkoutValidationService;
        _escrowService = escrowService;
    }

    /// <summary>
    /// Gets available shipping methods for the checkout shipping step.
    /// Filters methods based on delivery address region availability.
    /// </summary>
    public async Task<CheckoutShippingDto?> HandleAsync(
        GetCheckoutShippingQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Get cart
        var cart = await GetCartAsync(query.BuyerId, query.SessionId, cancellationToken);
        if (cart is null || cart.Items.Count == 0)
        {
            return null;
        }

        // Get delivery address for region filtering
        var deliveryAddress = await _deliveryAddressRepository.GetByIdAsync(query.DeliveryAddressId, cancellationToken);
        var deliveryCountry = deliveryAddress?.Country;

        // Get products for pricing
        var productIds = cart.Items.Select(i => i.ProductId).ToList();
        var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);
        var productLookup = products.ToDictionary(p => p.Id);

        // Get stores
        var storeIds = cart.Items.Select(i => i.StoreId).Distinct().ToList();
        var stores = await _storeRepository.GetByIdsAsync(storeIds, cancellationToken);
        var storeLookup = stores.ToDictionary(s => s.Id);

        // Get shipping methods for all stores
        var shippingMethodsByStore = await _shippingMethodRepository.GetByStoreIdsAsync(storeIds, cancellationToken);

        // Also get platform-wide methods as fallback
        var platformMethods = await _shippingMethodRepository.GetPlatformMethodsAsync(cancellationToken);

        // Group cart items by store and calculate options
        var sellerOptions = new List<SellerShippingOptionsDto>();
        var currency = products.FirstOrDefault()?.Price.Currency ?? "USD";
        decimal totalItemSubtotal = 0m;
        decimal totalShipping = 0m;

        var itemsByStore = cart.GetItemsGroupedBySeller();
        foreach (var (storeId, items) in itemsByStore)
        {
            var store = storeLookup.GetValueOrDefault(storeId);
            var storeName = store?.Name ?? "Unknown Seller";

            // Calculate subtotal and item count for this seller
            decimal sellerSubtotal = 0m;
            int itemCount = 0;
            foreach (var item in items)
            {
                var product = productLookup.GetValueOrDefault(item.ProductId);
                if (product is not null)
                {
                    sellerSubtotal += product.Price.Amount * item.Quantity;
                    itemCount += item.Quantity;
                }
            }

            totalItemSubtotal += sellerSubtotal;

            // Get shipping methods for this store (or platform defaults)
            var methods = shippingMethodsByStore.TryGetValue(storeId, out var storeMethods) && storeMethods.Any()
                ? storeMethods
                : platformMethods;

            // Filter methods based on delivery address region availability
            var availableMethods = methods
                .Where(m => m.IsActive && m.IsAvailableForRegion(deliveryCountry))
                .ToList();

            var methodDtos = availableMethods
                .OrderBy(m => m.DisplayOrder)
                .ThenBy(m => m.BaseCost)
                .Select(m => MapToShippingMethodDto(m, sellerSubtotal, itemCount))
                .ToList();

            // Find default method from available methods
            var defaultMethod = availableMethods.FirstOrDefault(m => m.IsDefault)
                ?? availableMethods.FirstOrDefault();

            if (defaultMethod is not null)
            {
                totalShipping += defaultMethod.CalculateCost(sellerSubtotal, itemCount);
            }

            sellerOptions.Add(new SellerShippingOptionsDto(
                storeId,
                storeName,
                methodDtos.AsReadOnly(),
                defaultMethod?.Id,
                sellerSubtotal,
                itemCount));
        }

        return new CheckoutShippingDto(
            sellerOptions.AsReadOnly(),
            totalItemSubtotal,
            totalShipping,
            totalItemSubtotal + totalShipping,
            currency);
    }

    /// <summary>
    /// Calculates shipping costs for selected methods.
    /// </summary>
    public async Task<SelectShippingResultDto> HandleAsync(
        SelectShippingMethodsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Get cart
        var cart = await GetCartAsync(command.BuyerId, command.SessionId, cancellationToken);
        if (cart is null || cart.Items.Count == 0)
        {
            return SelectShippingResultDto.Failed("Cart is empty.");
        }

        // Get products for pricing
        var productIds = cart.Items.Select(i => i.ProductId).ToList();
        var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);
        var productLookup = products.ToDictionary(p => p.Id);

        // Validate all stores have a shipping method selected
        var storeIds = cart.Items.Select(i => i.StoreId).Distinct().ToList();
        foreach (var storeId in storeIds)
        {
            if (!command.ShippingMethodsByStore.ContainsKey(storeId))
            {
                return SelectShippingResultDto.Failed($"Please select a shipping method for all sellers.");
            }
        }

        // Calculate totals
        decimal totalItemSubtotal = 0m;
        decimal totalShipping = 0m;

        var itemsByStore = cart.GetItemsGroupedBySeller();
        foreach (var (storeId, items) in itemsByStore)
        {
            // Calculate subtotal and item count for this seller
            decimal sellerSubtotal = 0m;
            int itemCount = 0;
            foreach (var item in items)
            {
                var product = productLookup.GetValueOrDefault(item.ProductId);
                if (product is not null)
                {
                    sellerSubtotal += product.Price.Amount * item.Quantity;
                    itemCount += item.Quantity;
                }
            }

            totalItemSubtotal += sellerSubtotal;

            // Get selected shipping method
            var selectedMethodId = command.ShippingMethodsByStore[storeId];
            var shippingMethod = await _shippingMethodRepository.GetByIdAsync(selectedMethodId, cancellationToken);
            if (shippingMethod is null || !shippingMethod.IsActive)
            {
                return SelectShippingResultDto.Failed("Selected shipping method is not available.");
            }

            totalShipping += shippingMethod.CalculateCost(sellerSubtotal, itemCount);
        }

        return SelectShippingResultDto.Succeeded(totalShipping, totalItemSubtotal + totalShipping);
    }

    /// <summary>
    /// Gets available payment methods for the checkout payment step.
    /// </summary>
    public async Task<CheckoutPaymentDto?> HandleAsync(
        GetCheckoutPaymentMethodsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Get cart
        var cart = await GetCartAsync(query.BuyerId, query.SessionId, cancellationToken);
        if (cart is null || cart.Items.Count == 0)
        {
            return null;
        }

        // Get delivery address
        var address = await _deliveryAddressRepository.GetByIdAsync(query.DeliveryAddressId, cancellationToken);
        string? addressSummary = address is not null
            ? $"{address.RecipientName}, {address.Street}, {address.City}, {address.Country}"
            : null;

        // Get products for pricing
        var productIds = cart.Items.Select(i => i.ProductId).ToList();
        var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);
        var productLookup = products.ToDictionary(p => p.Id);
        var currency = products.FirstOrDefault()?.Price.Currency ?? "USD";

        // Calculate totals with selected shipping
        decimal totalItemSubtotal = 0m;
        decimal totalShipping = 0m;

        var itemsByStore = cart.GetItemsGroupedBySeller();
        foreach (var (storeId, items) in itemsByStore)
        {
            decimal sellerSubtotal = 0m;
            int itemCount = 0;
            foreach (var item in items)
            {
                var product = productLookup.GetValueOrDefault(item.ProductId);
                if (product is not null)
                {
                    sellerSubtotal += product.Price.Amount * item.Quantity;
                    itemCount += item.Quantity;
                }
            }

            totalItemSubtotal += sellerSubtotal;

            // Get selected shipping method cost
            if (query.ShippingMethodsByStore.TryGetValue(storeId, out var methodId))
            {
                var method = await _shippingMethodRepository.GetByIdAsync(methodId, cancellationToken);
                if (method is not null)
                {
                    totalShipping += method.CalculateCost(sellerSubtotal, itemCount);
                }
            }
        }

        // Get active payment methods
        var paymentMethods = await _paymentMethodRepository.GetActiveAsync(cancellationToken);
        var methodDtos = paymentMethods
            .OrderBy(m => m.DisplayOrder)
            .Select(MapToPaymentMethodDto)
            .ToList();

        var defaultMethod = paymentMethods.FirstOrDefault(m => m.IsDefault)
            ?? paymentMethods.FirstOrDefault();

        return new CheckoutPaymentDto(
            methodDtos.AsReadOnly(),
            defaultMethod?.Id,
            totalItemSubtotal,
            totalShipping,
            totalItemSubtotal + totalShipping,
            currency,
            addressSummary);
    }

    /// <summary>
    /// Initiates payment and creates the order.
    /// Validates stock availability and price changes before order creation.
    /// </summary>
    public async Task<InitiatePaymentResultDto> HandleAsync(
        InitiatePaymentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Get cart
        var cart = await _cartRepository.GetByBuyerIdAsync(command.BuyerId, cancellationToken);
        if (cart is null || cart.Items.Count == 0)
        {
            return InitiatePaymentResultDto.Failed("Cart is empty.");
        }

        // Get delivery address
        var address = await _deliveryAddressRepository.GetByIdAsync(command.DeliveryAddressId, cancellationToken);
        if (address is null)
        {
            return InitiatePaymentResultDto.Failed("Delivery address not found.");
        }

        // Get payment method
        var paymentMethod = await _paymentMethodRepository.GetByIdAsync(command.PaymentMethodId, cancellationToken);
        if (paymentMethod is null || !paymentMethod.IsActive)
        {
            return InitiatePaymentResultDto.Failed("Payment method is not available.");
        }

        // Check if payment method is enabled in current environment
        if (!_paymentProviderService.IsPaymentMethodEnabled(paymentMethod.Type))
        {
            return InitiatePaymentResultDto.Failed($"Payment method '{paymentMethod.Name}' is not available in this environment.");
        }

        // Get products and stores
        var productIds = cart.Items.Select(i => i.ProductId).ToList();
        var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);
        var productLookup = products.ToDictionary(p => p.Id);
        var currency = products.FirstOrDefault()?.Price.Currency ?? "USD";

        // Validate stock availability and price changes before creating order
        var validationResult = _checkoutValidationService.ValidateCartItems(cart.Items, productLookup);

        if (!validationResult.IsValid)
        {
            var validationIssues = validationResult.ItemResults
                .Where(r => !r.IsValid)
                .Select(r => new CartItemValidationIssueDto(
                    r.ProductId,
                    r.ProductName,
                    !r.IsStockValid,
                    !r.IsPriceValid,
                    r.RequestedQuantity,
                    r.AvailableStock,
                    r.OriginalPrice,
                    r.CurrentPrice,
                    r.Currency,
                    r.StockValidationMessage,
                    r.PriceValidationMessage))
                .ToList()
                .AsReadOnly();

            return InitiatePaymentResultDto.ValidationFailed(
                validationResult.GetSummaryMessage(),
                validationResult.HasStockIssues,
                validationResult.HasPriceChanges,
                validationIssues);
        }

        var storeIds = cart.Items.Select(i => i.StoreId).Distinct().ToList();
        var stores = await _storeRepository.GetByIdsAsync(storeIds, cancellationToken);
        var storeLookup = stores.ToDictionary(s => s.Id);

        // Generate order number
        var orderNumber = await _orderRepository.GenerateOrderNumberAsync(cancellationToken);

        // Create order
        var order = new Order(
            command.BuyerId,
            orderNumber,
            command.DeliveryAddressId,
            address.RecipientName,
            address.Street,
            address.Street2,
            address.City,
            address.State,
            address.PostalCode,
            address.Country,
            address.PhoneNumber,
            null, // Delivery instructions - can be added via checkout extension
            command.PaymentMethodId,
            paymentMethod.Name,
            currency);

        // Add items with shipping - use current product prices for the order snapshot
        var itemsByStore = cart.GetItemsGroupedBySeller();
        foreach (var (storeId, items) in itemsByStore)
        {
            decimal sellerSubtotal = 0m;
            int itemCount = 0;

            // Calculate seller subtotal first for shipping cost
            foreach (var item in items)
            {
                var product = productLookup.GetValueOrDefault(item.ProductId);
                if (product is not null)
                {
                    sellerSubtotal += product.Price.Amount * item.Quantity;
                    itemCount += item.Quantity;
                }
            }

            // Get selected shipping method
            ShippingMethod? shippingMethod = null;
            decimal shippingCost = 0m;
            if (command.ShippingMethodsByStore.TryGetValue(storeId, out var methodId))
            {
                shippingMethod = await _shippingMethodRepository.GetByIdAsync(methodId, cancellationToken);
                if (shippingMethod is not null)
                {
                    // Validate shipping method is active and available for the delivery region
                    if (!shippingMethod.IsActive)
                    {
                        return InitiatePaymentResultDto.Failed($"Selected shipping method '{shippingMethod.Name}' is no longer available.");
                    }

                    if (!shippingMethod.IsAvailableForRegion(address.Country))
                    {
                        return InitiatePaymentResultDto.Failed($"Selected shipping method '{shippingMethod.Name}' is not available for delivery to {address.Country}.");
                    }

                    shippingCost = shippingMethod.CalculateCost(sellerSubtotal, itemCount);
                }
            }

            // Distribute shipping cost across items (for reporting purposes)
            var shippingPerItem = itemCount > 0 ? shippingCost / itemCount : 0m;

            foreach (var item in items)
            {
                var product = productLookup.GetValueOrDefault(item.ProductId);
                if (product is not null)
                {
                    // Calculate new stock level with protection against negative values
                    var newStock = product.Stock - item.Quantity;
                    if (newStock < 0)
                    {
                        // Stock was modified by another concurrent transaction
                        // Return a validation error instead of creating an invalid order
                        return InitiatePaymentResultDto.Failed(
                            $"Unable to complete order: '{product.Name}' has insufficient stock. Please refresh your cart and try again.");
                    }

                    // Store snapshot of current price and delivery time with the order
                    order.AddItem(
                        item.ProductId,
                        item.StoreId,
                        product.Name,
                        product.Price.Amount,
                        item.Quantity,
                        shippingMethod?.Id,
                        shippingMethod?.Name,
                        shippingPerItem * item.Quantity,
                        shippingMethod?.EstimatedDeliveryDaysMin,
                        shippingMethod?.EstimatedDeliveryDaysMax);

                    // Reduce stock for the product
                    product.UpdateStock(newStock);
                    _productRepository.Update(product);
                }
            }
        }

        // Create shipments grouped by seller
        order.CreateShipments();

        // Generate idempotency key for payment provider
        var idempotencyKey = $"order-{order.Id:N}-{DateTime.UtcNow.Ticks}";
        order.SetPaymentIdempotencyKey(idempotencyKey);

        // Save order and updated product stock
        // NOTE: When migrating to a relational database provider, wrap these operations
        // in a transaction (using TransactionScope or DbContext.Database.BeginTransactionAsync)
        // to ensure atomicity. The current InMemory provider does not support transactions.
        await _orderRepository.AddAsync(order, cancellationToken);
        await _productRepository.SaveChangesAsync(cancellationToken);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        // Initiate payment with the payment provider
        var returnUrl = command.ReturnUrl ?? $"/Buyer/Checkout/Confirmation/{order.Id}";
        var paymentResult = await _paymentProviderService.InitiatePaymentAsync(
            order.Id,
            order.TotalAmount,
            order.Currency,
            paymentMethod.Type,
            idempotencyKey,
            returnUrl,
            cancellationToken);

        if (!paymentResult.IsSuccess)
        {
            // Payment initiation failed - mark order as failed
            order.FailPayment();
            await _orderRepository.UpdateAsync(order, cancellationToken);
            await _orderRepository.SaveChangesAsync(cancellationToken);
            return InitiatePaymentResultDto.Failed(paymentResult.ErrorMessage ?? "Payment initiation failed.");
        }

        // Store the pending transaction ID
        if (paymentResult.TransactionId is not null)
        {
            order.SetPendingTransactionId(paymentResult.TransactionId);
            await _orderRepository.UpdateAsync(order, cancellationToken);
            await _orderRepository.SaveChangesAsync(cancellationToken);
        }

        // Handle BLIK - requires code entry
        if (paymentResult.RequiresBlikCode)
        {
            // Don't clear cart yet - user needs to enter BLIK code
            return InitiatePaymentResultDto.RequiresBlik(
                order.Id,
                order.OrderNumber,
                paymentResult.TransactionId!);
        }

        // Handle redirect-based payments (card, bank transfer)
        if (paymentResult.RequiresRedirect && paymentResult.RedirectUrl is not null)
        {
            // Don't clear cart yet - payment not confirmed
            return InitiatePaymentResultDto.Succeeded(order.Id, order.OrderNumber, paymentResult.RedirectUrl);
        }

        // For immediate success (e.g., digital wallet, cash on delivery)
        order.ConfirmPayment(paymentResult.TransactionId);
        await _orderRepository.UpdateAsync(order, cancellationToken);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        // Create escrow payment to hold funds
        await _escrowService.CreateEscrowForOrderAsync(order, cancellationToken);

        // Clear the cart after successful order
        cart.Clear();
        await _cartRepository.UpdateAsync(cart, cancellationToken);
        await _cartRepository.SaveChangesAsync(cancellationToken);

        // Send order confirmation notification with buyer details
        var buyer = await _userRepository.GetByIdAsync(command.BuyerId, cancellationToken);
        if (buyer is not null)
        {
            await _notificationService.SendOrderConfirmationAsync(
                order.Id,
                buyer.Email.Value,
                order.OrderNumber,
                order.TotalAmount,
                order.Currency,
                cancellationToken);
        }

        // Send notifications to sellers for their sub-orders
        await SendNewOrderNotificationsToSellersAsync(order, cancellationToken);

        return InitiatePaymentResultDto.Succeeded(order.Id, order.OrderNumber);
    }

    /// <summary>
    /// Submits a BLIK code to complete payment.
    /// </summary>
    public async Task<SubmitBlikCodeResultDto> HandleAsync(
        SubmitBlikCodeCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Get the order
        var order = await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            return SubmitBlikCodeResultDto.Failed("Order not found.");
        }

        // Verify the order belongs to the buyer
        if (order.BuyerId != command.BuyerId)
        {
            return SubmitBlikCodeResultDto.Failed("Order not found.");
        }

        // Verify order is in pending status
        if (order.Status != OrderStatus.Pending)
        {
            return SubmitBlikCodeResultDto.Failed("Order is not in pending payment status.");
        }

        // Verify we have a pending transaction ID
        if (string.IsNullOrEmpty(order.PaymentTransactionId))
        {
            return SubmitBlikCodeResultDto.Failed("No pending payment found for this order.");
        }

        // Generate idempotency key for BLIK submission (without including sensitive BLIK code)
        var idempotencyKey = $"blik-{order.Id:N}-{DateTime.UtcNow.Ticks}";

        // Submit BLIK code to payment provider
        var blikResult = await _paymentProviderService.SubmitBlikCodeAsync(
            order.Id,
            order.PaymentTransactionId,
            command.BlikCode,
            idempotencyKey,
            cancellationToken);

        if (!blikResult.IsSuccess)
        {
            // BLIK payment failed
            return SubmitBlikCodeResultDto.Failed(blikResult.ErrorMessage ?? "BLIK payment failed.");
        }

        // Payment successful - confirm the order
        order.ConfirmPayment(blikResult.TransactionId);
        await _orderRepository.UpdateAsync(order, cancellationToken);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        // Create escrow payment to hold funds
        await _escrowService.CreateEscrowForOrderAsync(order, cancellationToken);

        // Clear the buyer's cart
        var cart = await _cartRepository.GetByBuyerIdAsync(command.BuyerId, cancellationToken);
        if (cart is not null)
        {
            cart.Clear();
            await _cartRepository.UpdateAsync(cart, cancellationToken);
            await _cartRepository.SaveChangesAsync(cancellationToken);
        }

        // Send order confirmation notification
        var buyer = await _userRepository.GetByIdAsync(command.BuyerId, cancellationToken);
        if (buyer is not null)
        {
            await _notificationService.SendOrderConfirmationAsync(
                order.Id,
                buyer.Email.Value,
                order.OrderNumber,
                order.TotalAmount,
                order.Currency,
                cancellationToken);
        }

        // Send notifications to sellers for their sub-orders
        await SendNewOrderNotificationsToSellersAsync(order, cancellationToken);

        return SubmitBlikCodeResultDto.Succeeded(order.Id, order.OrderNumber);
    }

    /// <summary>
    /// Confirms payment after return from payment provider.
    /// Verifies the payment status with the provider and updates order accordingly.
    /// </summary>
    public async Task<ConfirmPaymentResultDto> HandleAsync(
        ConfirmPaymentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var order = await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            return ConfirmPaymentResultDto.Failed("Order not found.");
        }

        // If transaction ID provided, verify with payment provider
        if (!string.IsNullOrEmpty(command.TransactionId) && order.Status == OrderStatus.Pending)
        {
            var confirmationResult = await _paymentProviderService.ConfirmPaymentAsync(
                order.Id,
                command.TransactionId,
                cancellationToken);

            if (confirmationResult.Status == PaymentConfirmationStatus.Completed)
            {
                order.ConfirmPayment(confirmationResult.TransactionId);
                await _orderRepository.UpdateAsync(order, cancellationToken);
                await _orderRepository.SaveChangesAsync(cancellationToken);

                // Create escrow payment to hold funds
                await _escrowService.CreateEscrowForOrderAsync(order, cancellationToken);

                // Clear the buyer's cart
                var cart = await _cartRepository.GetByBuyerIdAsync(order.BuyerId, cancellationToken);
                if (cart is not null)
                {
                    cart.Clear();
                    await _cartRepository.UpdateAsync(cart, cancellationToken);
                    await _cartRepository.SaveChangesAsync(cancellationToken);
                }

                // Send order confirmation notification
                var buyer = await _userRepository.GetByIdAsync(order.BuyerId, cancellationToken);
                if (buyer is not null)
                {
                    await _notificationService.SendOrderConfirmationAsync(
                        order.Id,
                        buyer.Email.Value,
                        order.OrderNumber,
                        order.TotalAmount,
                        order.Currency,
                        cancellationToken);
                }

                // Send notifications to sellers for their sub-orders
                await SendNewOrderNotificationsToSellersAsync(order, cancellationToken);

                return ConfirmPaymentResultDto.Succeeded(order.Id, order.OrderNumber);
            }
            else if (confirmationResult.Status == PaymentConfirmationStatus.Failed ||
                     confirmationResult.Status == PaymentConfirmationStatus.Cancelled ||
                     confirmationResult.Status == PaymentConfirmationStatus.Expired)
            {
                order.FailPayment();
                await _orderRepository.UpdateAsync(order, cancellationToken);
                await _orderRepository.SaveChangesAsync(cancellationToken);
                return ConfirmPaymentResultDto.Failed(confirmationResult.ErrorMessage ?? "Payment was not successful.");
            }
            else
            {
                // Payment still pending
                return ConfirmPaymentResultDto.Failed("Payment is still being processed. Please wait.");
            }
        }

        // Fallback to simple boolean confirmation (for backwards compatibility)
        if (command.IsSuccessful)
        {
            if (order.Status == OrderStatus.Pending)
            {
                order.ConfirmPayment(command.TransactionId);
                await _orderRepository.UpdateAsync(order, cancellationToken);
                await _orderRepository.SaveChangesAsync(cancellationToken);
            }

            return ConfirmPaymentResultDto.Succeeded(order.Id, order.OrderNumber);
        }
        else
        {
            if (order.Status == OrderStatus.Pending)
            {
                order.FailPayment();
                await _orderRepository.UpdateAsync(order, cancellationToken);
                await _orderRepository.SaveChangesAsync(cancellationToken);
            }

            return ConfirmPaymentResultDto.Failed("Payment was not successful. Please try again.");
        }
    }

    /// <summary>
    /// Gets order confirmation details.
    /// </summary>
    public async Task<OrderConfirmationDto?> HandleAsync(
        GetOrderConfirmationQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var order = await _orderRepository.GetByIdAsync(query.OrderId, cancellationToken);
        if (order is null || order.BuyerId != query.BuyerId)
        {
            return null;
        }

        // Get store names
        var storeIds = order.Items.Select(i => i.StoreId).Distinct().ToList();
        var stores = await _storeRepository.GetByIdsAsync(storeIds, cancellationToken);
        var storeLookup = stores.ToDictionary(s => s.Id);

        // Get shipping methods for estimated delivery
        var shippingMethodIds = order.Items
            .Where(i => i.ShippingMethodId.HasValue)
            .Select(i => i.ShippingMethodId!.Value)
            .Distinct()
            .ToList();
        var shippingMethods = await _shippingMethodRepository.GetByIdsAsync(shippingMethodIds, cancellationToken);
        var shippingMethodLookup = shippingMethods.ToDictionary(m => m.Id);

        // Get buyer email for confirmation
        var buyer = await _userRepository.GetByIdAsync(order.BuyerId, cancellationToken);
        var buyerEmail = buyer?.Email.Value;

        var addressSummary = $"{order.DeliveryStreet}, {order.DeliveryCity}, {order.DeliveryPostalCode}, {order.DeliveryCountry}";

        var itemDtos = order.Items.Select(i =>
        {
            // Prefer stored delivery time (historical value at order creation)
            // Fall back to current shipping method value for backwards compatibility
            string? estimatedDelivery = i.GetEstimatedDeliveryDisplay();
            if (estimatedDelivery is null 
                && i.ShippingMethodId.HasValue 
                && shippingMethodLookup.TryGetValue(i.ShippingMethodId.Value, out var method))
            {
                estimatedDelivery = method.GetDeliveryTimeDisplay();
            }

            return new OrderItemDto(
                i.Id,
                i.ProductId,
                i.ProductName,
                i.StoreId,
                storeLookup.GetValueOrDefault(i.StoreId)?.Name ?? "Unknown Seller",
                i.UnitPrice,
                i.Quantity,
                i.LineTotal,
                i.ShippingMethodName,
                i.ShippingCost,
                estimatedDelivery,
                i.Status.ToString(),
                i.CarrierName,
                i.TrackingNumber,
                i.TrackingUrl,
                i.ShippedAt,
                i.DeliveredAt,
                i.CancelledAt,
                i.RefundedAt,
                i.RefundedAmount);
        }).ToList();

        // Calculate overall estimated delivery range from stored order item values
        // Fall back to current shipping method values for backwards compatibility
        string? estimatedDeliveryRange = null;
        var itemsWithDeliveryTime = order.Items
            .Where(i => i.EstimatedDeliveryDaysMin.HasValue && i.EstimatedDeliveryDaysMax.HasValue)
            .ToList();
        
        if (itemsWithDeliveryTime.Count > 0)
        {
            var minDays = itemsWithDeliveryTime.Min(i => i.EstimatedDeliveryDaysMin!.Value);
            var maxDays = itemsWithDeliveryTime.Max(i => i.EstimatedDeliveryDaysMax!.Value);
            var minDate = order.CreatedAt.AddDays(minDays).ToString("MMM dd");
            var maxDate = order.CreatedAt.AddDays(maxDays).ToString("MMM dd, yyyy");
            estimatedDeliveryRange = $"{minDate} - {maxDate}";
        }
        else if (shippingMethods.Count > 0)
        {
            // Backwards compatibility: fall back to current shipping method values
            var minDays = shippingMethods.Min(m => m.EstimatedDeliveryDaysMin);
            var maxDays = shippingMethods.Max(m => m.EstimatedDeliveryDaysMax);
            var minDate = order.CreatedAt.AddDays(minDays).ToString("MMM dd");
            var maxDate = order.CreatedAt.AddDays(maxDays).ToString("MMM dd, yyyy");
            estimatedDeliveryRange = $"{minDate} - {maxDate}";
        }

        return new OrderConfirmationDto(
            order.Id,
            order.OrderNumber,
            order.Status.ToString(),
            order.PaymentStatus.ToString(),
            PaymentStatusMapper.GetBuyerFriendlyMessage(order.PaymentStatus),
            order.RecipientName,
            addressSummary,
            order.PaymentMethodName,
            itemDtos.AsReadOnly(),
            order.ItemSubtotal,
            order.TotalShipping,
            order.TotalAmount,
            order.Currency,
            order.CreatedAt,
            estimatedDeliveryRange,
            buyerEmail,
            order.RefundedAmount);
    }

    /// <summary>
    /// Gets buyer's order history with pagination.
    /// </summary>
    public async Task<IReadOnlyList<OrderSummaryDto>> HandleAsync(
        GetBuyerOrdersQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var orders = await _orderRepository.GetRecentByBuyerIdAsync(
            query.BuyerId,
            query.Skip,
            query.Take,
            cancellationToken);

        return orders.Select(o => new OrderSummaryDto(
            o.Id,
            o.OrderNumber,
            o.Status.ToString(),
            o.Items.Sum(i => i.Quantity),
            o.TotalAmount,
            o.Currency,
            o.CreatedAt)).ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets buyer's orders with filtering and pagination.
    /// </summary>
    public async Task<PagedResultDto<OrderSummaryDto>> HandleAsync(
        GetFilteredBuyerOrdersQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Validate and normalize pagination
        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var skip = (pageNumber - 1) * pageSize;

        // Parse status filter if provided
        OrderStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<OrderStatus>(query.Status, ignoreCase: true, out var parsedStatus))
        {
            statusFilter = parsedStatus;
        }

        // Get filtered orders from repository
        var (orders, totalCount) = await _orderRepository.GetFilteredByBuyerIdAsync(
            query.BuyerId,
            statusFilter,
            query.FromDate,
            query.ToDate,
            query.SellerId,
            skip,
            pageSize,
            cancellationToken);

        var orderDtos = orders.Select(o => new OrderSummaryDto(
            o.Id,
            o.OrderNumber,
            o.Status.ToString(),
            o.Items.Sum(i => i.Quantity),
            o.TotalAmount,
            o.Currency,
            o.CreatedAt)).ToList().AsReadOnly();

        return PagedResultDto<OrderSummaryDto>.Create(
            orderDtos,
            pageNumber,
            pageSize,
            totalCount);
    }

    private async Task<Cart?> GetCartAsync(Guid? buyerId, string? sessionId, CancellationToken cancellationToken)
    {
        if (buyerId.HasValue && buyerId.Value != Guid.Empty)
        {
            return await _cartRepository.GetByBuyerIdAsync(buyerId.Value, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            return await _cartRepository.GetBySessionIdAsync(sessionId, cancellationToken);
        }

        return null;
    }

    private static ShippingMethodDto MapToShippingMethodDto(ShippingMethod method, decimal subtotal, int itemCount)
    {
        var cost = method.CalculateCost(subtotal, itemCount);
        return new ShippingMethodDto(
            method.Id,
            method.StoreId,
            method.Name,
            method.Description,
            method.CarrierName,
            method.EstimatedDeliveryDaysMin,
            method.EstimatedDeliveryDaysMax,
            method.GetDeliveryTimeDisplay(),
            cost,
            method.Currency,
            cost == 0m,
            method.IsDefault);
    }

    private static PaymentMethodDto MapToPaymentMethodDto(PaymentMethod method)
    {
        return new PaymentMethodDto(
            method.Id,
            method.Name,
            method.Description,
            method.Type.ToString(),
            method.IconClass,
            method.IsDefault);
    }

    /// <summary>
    /// Sends new order notifications to all sellers who have items in the order.
    /// </summary>
    private async Task SendNewOrderNotificationsToSellersAsync(Order order, CancellationToken cancellationToken)
    {
        // Get all store IDs from the order shipments
        var storeIds = order.Shipments.Select(s => s.StoreId).Distinct().ToList();
        var stores = await _storeRepository.GetByIdsAsync(storeIds, cancellationToken);

        // Batch fetch all sellers for the stores
        var sellerIds = stores.Select(s => s.SellerId).Distinct().ToList();
        var sellers = await _userRepository.GetByIdsAsync(sellerIds, cancellationToken);
        var sellerLookup = sellers.ToDictionary(s => s.Id);

        foreach (var shipment in order.Shipments)
        {
            var store = stores.FirstOrDefault(s => s.Id == shipment.StoreId);
            if (store is null)
            {
                continue;
            }

            // Get the seller for this store from cached lookup
            if (!sellerLookup.TryGetValue(store.SellerId, out var seller) || seller.Email is null)
            {
                continue;
            }

            // Count items for this seller
            var itemCount = order.Items.Where(i => i.StoreId == shipment.StoreId).Sum(i => i.Quantity);

            await _notificationService.SendNewOrderNotificationToSellerAsync(
                order.Id,
                shipment.Id,
                seller.Email.Value,
                order.OrderNumber,
                itemCount,
                shipment.Subtotal,
                order.Currency,
                cancellationToken);
        }
    }
}
