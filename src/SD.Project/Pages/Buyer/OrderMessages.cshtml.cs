using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Commands;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;
using System.Security.Claims;

namespace SD.Project.Pages.Buyer;

/// <summary>
/// Page model for displaying and sending order messages.
/// </summary>
[RequireRole(UserRole.Buyer, UserRole.Seller, UserRole.Admin)]
public class OrderMessagesModel : PageModel
{
    private readonly ILogger<OrderMessagesModel> _logger;
    private readonly OrderMessageService _messageService;
    private readonly OrderService _orderService;

    public OrderMessageThreadViewModel? Thread { get; private set; }
    public IReadOnlyList<OrderMessageThreadSummaryViewModel> Threads { get; private set; } = [];
    public string? Message { get; private set; }
    public bool IsSuccess { get; private set; }

    [BindProperty(SupportsGet = true)]
    public Guid? OrderId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? StoreId { get; set; }

    [BindProperty]
    public SendOrderMessageInputModel MessageInput { get; set; } = new();

    public OrderMessagesModel(
        ILogger<OrderMessagesModel> logger,
        OrderMessageService messageService,
        OrderService orderService)
    {
        _logger = logger;
        _messageService = messageService;
        _orderService = orderService;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Login");
        }

        if (OrderId.HasValue && StoreId.HasValue)
        {
            // Load specific thread
            var threadDto = await _messageService.HandleAsync(
                new GetOrderMessageThreadQuery(OrderId.Value, StoreId.Value, userId, "buyer"),
                cancellationToken);

            if (threadDto is not null)
            {
                Thread = new OrderMessageThreadViewModel(
                    threadDto.OrderId,
                    threadDto.OrderNumber,
                    threadDto.StoreId,
                    threadDto.StoreName,
                    threadDto.Messages.Select(m => new OrderMessageViewModel(
                        m.Id,
                        m.OrderId,
                        m.StoreId,
                        m.SenderRole,
                        m.SenderName,
                        m.Content,
                        m.SentAt,
                        m.IsRead)).ToList().AsReadOnly(),
                    threadDto.UnreadCount);

                // Mark messages as read
                await _messageService.HandleAsync(
                    new MarkOrderMessagesReadCommand(OrderId.Value, StoreId.Value, userId, "buyer"),
                    cancellationToken);

                _logger.LogInformation("Loaded message thread for order {OrderId}, store {StoreId}", OrderId.Value, StoreId.Value);
            }
        }
        else
        {
            // Load thread list
            var threadDtos = await _messageService.HandleAsync(
                new GetBuyerMessageThreadsQuery(userId),
                cancellationToken);

            Threads = threadDtos.Select(t => new OrderMessageThreadSummaryViewModel(
                t.OrderId,
                t.OrderNumber,
                t.StoreId,
                t.StoreName,
                t.LastMessagePreview,
                t.LastMessageAt,
                t.UnreadCount)).ToList().AsReadOnly();

            _logger.LogInformation("Loaded {Count} message threads for buyer {BuyerId}", Threads.Count, userId);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSendMessageAsync(CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Login");
        }

        var result = await _messageService.HandleAsync(
            new SendOrderMessageCommand(
                MessageInput.OrderId, 
                MessageInput.StoreId, 
                userId, 
                "buyer", 
                MessageInput.Content),
            cancellationToken);

        if (result.IsSuccess)
        {
            IsSuccess = true;
            Message = "Your message has been sent.";
            MessageInput = new SendOrderMessageInputModel(); // Clear the form
            _logger.LogInformation("Message sent for order {OrderId} by buyer {BuyerId}", MessageInput.OrderId, userId);
        }
        else
        {
            IsSuccess = false;
            Message = result.ErrorMessage;
            _logger.LogWarning("Failed to send message for order {OrderId}: {Error}", MessageInput.OrderId, result.ErrorMessage);
        }

        // Reload with the thread
        OrderId = MessageInput.OrderId;
        StoreId = MessageInput.StoreId;
        return await OnGetAsync(cancellationToken);
    }
}
