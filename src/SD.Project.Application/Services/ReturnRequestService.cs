using SD.Project.Application.Commands;
using SD.Project.Application.DTOs;
using SD.Project.Application.Interfaces;
using SD.Project.Application.Queries;
using SD.Project.Domain.Entities;
using SD.Project.Domain.Repositories;

namespace SD.Project.Application.Services;

/// <summary>
/// Application service for return request operations.
/// Handles return initiation, eligibility checks, and status updates.
/// </summary>
public sealed class ReturnRequestService
{
    private readonly IReturnRequestRepository _returnRequestRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;

    /// <summary>
    /// Number of days after delivery that a return can be initiated.
    /// </summary>
    private const int ReturnWindowDays = 30;

    public ReturnRequestService(
        IReturnRequestRepository returnRequestRepository,
        IOrderRepository orderRepository,
        IStoreRepository storeRepository,
        IUserRepository userRepository,
        INotificationService notificationService)
    {
        _returnRequestRepository = returnRequestRepository;
        _orderRepository = orderRepository;
        _storeRepository = storeRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Checks if a sub-order is eligible for return.
    /// </summary>
    public async Task<ReturnEligibilityDto> HandleAsync(
        CheckReturnEligibilityQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Get the order
        var order = await _orderRepository.GetByIdAsync(query.OrderId, cancellationToken);
        if (order is null || order.BuyerId != query.BuyerId)
        {
            return new ReturnEligibilityDto(false, "Order not found or access denied.", null, false, null);
        }

        // Find the shipment
        var shipment = order.Shipments.FirstOrDefault(s => s.Id == query.ShipmentId);
        if (shipment is null)
        {
            return new ReturnEligibilityDto(false, "Sub-order not found.", null, false, null);
        }

        // Check if return request already exists
        var existingRequest = await _returnRequestRepository.GetByShipmentIdAsync(query.ShipmentId, cancellationToken);
        if (existingRequest is not null)
        {
            return new ReturnEligibilityDto(
                false,
                "A return request has already been submitted for this sub-order.",
                null,
                true,
                existingRequest.Status.ToString());
        }

        // Check if shipment is delivered
        if (shipment.Status != ShipmentStatus.Delivered)
        {
            return new ReturnEligibilityDto(
                false,
                "Returns can only be initiated for delivered orders.",
                null,
                false,
                null);
        }

        // Check if within return window
        if (!shipment.DeliveredAt.HasValue)
        {
            return new ReturnEligibilityDto(false, "Delivery date not recorded.", null, false, null);
        }

        var returnWindowEnds = shipment.DeliveredAt.Value.AddDays(ReturnWindowDays);
        if (DateTime.UtcNow > returnWindowEnds)
        {
            return new ReturnEligibilityDto(
                false,
                $"The return window has expired. Returns must be initiated within {ReturnWindowDays} days of delivery.",
                returnWindowEnds,
                false,
                null);
        }

        // Check if order/shipment is cancelled or refunded
        if (shipment.Status == ShipmentStatus.Cancelled || shipment.Status == ShipmentStatus.Refunded)
        {
            return new ReturnEligibilityDto(
                false,
                "Returns cannot be initiated for cancelled or refunded orders.",
                null,
                false,
                null);
        }

        return new ReturnEligibilityDto(true, null, returnWindowEnds, false, null);
    }

    /// <summary>
    /// Initiates a return request for a sub-order.
    /// </summary>
    public async Task<InitiateReturnResultDto> HandleAsync(
        InitiateReturnRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Check eligibility
        var eligibility = await HandleAsync(
            new CheckReturnEligibilityQuery(command.BuyerId, command.OrderId, command.ShipmentId),
            cancellationToken);

        if (!eligibility.IsEligible)
        {
            return new InitiateReturnResultDto(false, eligibility.IneligibilityReason);
        }

        // Get order and shipment for store ID
        var order = await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            return new InitiateReturnResultDto(false, "Order not found.");
        }

        var shipment = order.Shipments.FirstOrDefault(s => s.Id == command.ShipmentId);
        if (shipment is null)
        {
            return new InitiateReturnResultDto(false, "Sub-order not found.");
        }

        // Create the return request
        var returnRequest = new ReturnRequest(
            command.OrderId,
            command.ShipmentId,
            command.BuyerId,
            shipment.StoreId,
            command.Reason,
            command.Comments);

        await _returnRequestRepository.AddAsync(returnRequest, cancellationToken);
        await _returnRequestRepository.SaveChangesAsync(cancellationToken);

        // Send notification to seller
        var store = await _storeRepository.GetByIdAsync(shipment.StoreId, cancellationToken);
        if (store is not null)
        {
            var seller = await _userRepository.GetByIdAsync(store.SellerId, cancellationToken);
            if (seller?.Email is not null)
            {
                await _notificationService.SendReturnRequestCreatedAsync(
                    returnRequest.Id,
                    order.OrderNumber,
                    seller.Email.Value,
                    command.Reason,
                    cancellationToken);
            }
        }

        return new InitiateReturnResultDto(true, null, returnRequest.Id);
    }

    /// <summary>
    /// Gets return request by shipment ID for buyer display.
    /// </summary>
    public async Task<BuyerReturnRequestDto?> HandleAsync(
        GetReturnRequestByShipmentQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var returnRequest = await _returnRequestRepository.GetByShipmentIdAsync(query.ShipmentId, cancellationToken);
        if (returnRequest is null || returnRequest.BuyerId != query.BuyerId)
        {
            return null;
        }

        // Get order info
        var order = await _orderRepository.GetByIdAsync(returnRequest.OrderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        // Get store name
        var store = await _storeRepository.GetByIdAsync(returnRequest.StoreId, cancellationToken);
        var storeName = store?.Name ?? "Unknown Store";

        return new BuyerReturnRequestDto(
            returnRequest.Id,
            returnRequest.OrderId,
            returnRequest.ShipmentId,
            order.OrderNumber,
            storeName,
            returnRequest.Status.ToString(),
            returnRequest.Reason,
            returnRequest.Comments,
            returnRequest.SellerResponse,
            returnRequest.CreatedAt,
            returnRequest.ApprovedAt,
            returnRequest.RejectedAt,
            returnRequest.CompletedAt);
    }

    /// <summary>
    /// Gets all return requests for a buyer.
    /// </summary>
    public async Task<IReadOnlyList<BuyerReturnRequestDto>> HandleAsync(
        GetBuyerReturnRequestsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var returnRequests = await _returnRequestRepository.GetByBuyerIdAsync(query.BuyerId, cancellationToken);
        if (returnRequests.Count == 0)
        {
            return Array.Empty<BuyerReturnRequestDto>();
        }

        var result = new List<BuyerReturnRequestDto>();
        foreach (var request in returnRequests)
        {
            var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
            var store = await _storeRepository.GetByIdAsync(request.StoreId, cancellationToken);

            result.Add(new BuyerReturnRequestDto(
                request.Id,
                request.OrderId,
                request.ShipmentId,
                order?.OrderNumber ?? "Unknown",
                store?.Name ?? "Unknown Store",
                request.Status.ToString(),
                request.Reason,
                request.Comments,
                request.SellerResponse,
                request.CreatedAt,
                request.ApprovedAt,
                request.RejectedAt,
                request.CompletedAt));
        }

        return result.AsReadOnly();
    }

    /// <summary>
    /// Gets filtered return requests for a seller.
    /// </summary>
    public async Task<PagedResultDto<SellerReturnRequestSummaryDto>> HandleAsync(
        GetSellerReturnRequestsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var skip = (pageNumber - 1) * pageSize;

        // Parse status filter
        ReturnRequestStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<ReturnRequestStatus>(query.Status, ignoreCase: true, out var parsedStatus))
        {
            statusFilter = parsedStatus;
        }

        var (requests, totalCount) = await _returnRequestRepository.GetFilteredByStoreIdAsync(
            query.StoreId,
            statusFilter,
            query.FromDate,
            query.ToDate,
            skip,
            pageSize,
            cancellationToken);

        if (requests.Count == 0)
        {
            return PagedResultDto<SellerReturnRequestSummaryDto>.Create(
                Array.Empty<SellerReturnRequestSummaryDto>(),
                pageNumber,
                pageSize,
                totalCount);
        }

        var result = new List<SellerReturnRequestSummaryDto>();
        foreach (var request in requests)
        {
            var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
            var shipment = order?.Shipments.FirstOrDefault(s => s.Id == request.ShipmentId);
            var buyer = await _userRepository.GetByIdAsync(request.BuyerId, cancellationToken);

            var buyerName = buyer?.FirstName is not null && buyer?.LastName is not null
                ? $"{buyer.FirstName} {buyer.LastName}"
                : order?.RecipientName ?? "Unknown";

            var subOrderTotal = shipment is not null ? shipment.Subtotal + shipment.ShippingCost : 0m;

            result.Add(new SellerReturnRequestSummaryDto(
                request.Id,
                request.OrderId,
                order?.OrderNumber ?? "Unknown",
                request.Status.ToString(),
                buyerName,
                request.Reason,
                subOrderTotal,
                order?.Currency ?? "USD",
                request.CreatedAt));
        }

        return PagedResultDto<SellerReturnRequestSummaryDto>.Create(
            result.AsReadOnly(),
            pageNumber,
            pageSize,
            totalCount);
    }

    /// <summary>
    /// Gets details of a specific return request for a seller.
    /// </summary>
    public async Task<SellerReturnRequestDto?> HandleAsync(
        GetSellerReturnRequestDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var returnRequest = await _returnRequestRepository.GetByIdAsync(query.ReturnRequestId, cancellationToken);
        if (returnRequest is null || returnRequest.StoreId != query.StoreId)
        {
            return null;
        }

        var order = await _orderRepository.GetByIdAsync(returnRequest.OrderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        var shipment = order.Shipments.FirstOrDefault(s => s.Id == returnRequest.ShipmentId);
        var buyer = await _userRepository.GetByIdAsync(returnRequest.BuyerId, cancellationToken);

        var buyerName = buyer?.FirstName is not null && buyer?.LastName is not null
            ? $"{buyer.FirstName} {buyer.LastName}"
            : order.RecipientName;

        var subOrderTotal = shipment is not null ? shipment.Subtotal + shipment.ShippingCost : 0m;

        // Get items for this shipment
        var items = order.Items
            .Where(i => i.StoreId == returnRequest.StoreId)
            .Select(i => new SellerSubOrderItemDto(
                i.Id,
                i.ProductId,
                i.ProductName,
                i.UnitPrice,
                i.Quantity,
                i.LineTotal,
                i.ShippingMethodName,
                i.Status.ToString(),
                i.CarrierName,
                i.TrackingNumber,
                i.TrackingUrl,
                i.ShippedAt,
                i.DeliveredAt,
                i.CancelledAt,
                i.RefundedAt,
                i.RefundedAmount))
            .ToList();

        return new SellerReturnRequestDto(
            returnRequest.Id,
            returnRequest.OrderId,
            returnRequest.ShipmentId,
            order.OrderNumber,
            returnRequest.Status.ToString(),
            buyerName,
            buyer?.Email?.Value,
            returnRequest.Reason,
            returnRequest.Comments,
            returnRequest.SellerResponse,
            subOrderTotal,
            order.Currency,
            returnRequest.CreatedAt,
            returnRequest.ApprovedAt,
            returnRequest.RejectedAt,
            returnRequest.CompletedAt,
            items.AsReadOnly());
    }

    /// <summary>
    /// Gets return request by shipment ID for a seller.
    /// </summary>
    public async Task<SellerReturnRequestDto?> HandleAsync(
        GetSellerReturnRequestByShipmentQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var returnRequest = await _returnRequestRepository.GetByShipmentIdAsync(query.ShipmentId, cancellationToken);
        if (returnRequest is null || returnRequest.StoreId != query.StoreId)
        {
            return null;
        }

        // Delegate to the existing details query handler
        return await HandleAsync(
            new GetSellerReturnRequestDetailsQuery(query.StoreId, returnRequest.Id),
            cancellationToken);
    }

    /// <summary>
    /// Approves a return request.
    /// </summary>
    public async Task<UpdateReturnRequestResultDto> HandleAsync(
        ApproveReturnRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var returnRequest = await _returnRequestRepository.GetByIdAsync(command.ReturnRequestId, cancellationToken);
        if (returnRequest is null || returnRequest.StoreId != command.StoreId)
        {
            return new UpdateReturnRequestResultDto(false, "Return request not found.");
        }

        var previousStatus = returnRequest.Status.ToString();

        try
        {
            returnRequest.Approve(command.SellerResponse);
        }
        catch (InvalidOperationException ex)
        {
            return new UpdateReturnRequestResultDto(false, ex.Message);
        }

        await _returnRequestRepository.SaveChangesAsync(cancellationToken);

        // Send notification to buyer
        var order = await _orderRepository.GetByIdAsync(returnRequest.OrderId, cancellationToken);
        var buyer = await _userRepository.GetByIdAsync(returnRequest.BuyerId, cancellationToken);
        if (buyer?.Email is not null && order is not null)
        {
            await _notificationService.SendReturnRequestApprovedAsync(
                returnRequest.Id,
                order.OrderNumber,
                buyer.Email.Value,
                command.SellerResponse,
                cancellationToken);
        }

        return new UpdateReturnRequestResultDto(true, null, previousStatus, returnRequest.Status.ToString());
    }

    /// <summary>
    /// Rejects a return request.
    /// </summary>
    public async Task<UpdateReturnRequestResultDto> HandleAsync(
        RejectReturnRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var returnRequest = await _returnRequestRepository.GetByIdAsync(command.ReturnRequestId, cancellationToken);
        if (returnRequest is null || returnRequest.StoreId != command.StoreId)
        {
            return new UpdateReturnRequestResultDto(false, "Return request not found.");
        }

        var previousStatus = returnRequest.Status.ToString();

        try
        {
            returnRequest.Reject(command.RejectionReason);
        }
        catch (InvalidOperationException ex)
        {
            return new UpdateReturnRequestResultDto(false, ex.Message);
        }
        catch (ArgumentException ex)
        {
            return new UpdateReturnRequestResultDto(false, ex.Message);
        }

        await _returnRequestRepository.SaveChangesAsync(cancellationToken);

        // Send notification to buyer
        var order = await _orderRepository.GetByIdAsync(returnRequest.OrderId, cancellationToken);
        var buyer = await _userRepository.GetByIdAsync(returnRequest.BuyerId, cancellationToken);
        if (buyer?.Email is not null && order is not null)
        {
            await _notificationService.SendReturnRequestRejectedAsync(
                returnRequest.Id,
                order.OrderNumber,
                buyer.Email.Value,
                command.RejectionReason,
                cancellationToken);
        }

        return new UpdateReturnRequestResultDto(true, null, previousStatus, returnRequest.Status.ToString());
    }

    /// <summary>
    /// Completes a return request.
    /// </summary>
    public async Task<UpdateReturnRequestResultDto> HandleAsync(
        CompleteReturnRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var returnRequest = await _returnRequestRepository.GetByIdAsync(command.ReturnRequestId, cancellationToken);
        if (returnRequest is null || returnRequest.StoreId != command.StoreId)
        {
            return new UpdateReturnRequestResultDto(false, "Return request not found.");
        }

        var previousStatus = returnRequest.Status.ToString();

        try
        {
            returnRequest.Complete();
        }
        catch (InvalidOperationException ex)
        {
            return new UpdateReturnRequestResultDto(false, ex.Message);
        }

        await _returnRequestRepository.SaveChangesAsync(cancellationToken);

        // Send notification to buyer
        var order = await _orderRepository.GetByIdAsync(returnRequest.OrderId, cancellationToken);
        var buyer = await _userRepository.GetByIdAsync(returnRequest.BuyerId, cancellationToken);
        if (buyer?.Email is not null && order is not null)
        {
            await _notificationService.SendReturnRequestCompletedAsync(
                returnRequest.Id,
                order.OrderNumber,
                buyer.Email.Value,
                cancellationToken);
        }

        return new UpdateReturnRequestResultDto(true, null, previousStatus, returnRequest.Status.ToString());
    }
}
