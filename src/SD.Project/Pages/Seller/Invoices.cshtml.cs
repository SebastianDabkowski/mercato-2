using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;
using SD.Project.ViewModels;

namespace SD.Project.Pages.Seller;

/// <summary>
/// Page model for displaying seller's commission invoices.
/// </summary>
[RequireRole(UserRole.Seller, UserRole.Admin)]
public class InvoicesModel : PageModel
{
    private readonly ILogger<InvoicesModel> _logger;
    private readonly CommissionInvoiceService _invoiceService;
    private readonly StoreService _storeService;

    public IReadOnlyList<CommissionInvoiceListItemViewModel> Invoices { get; private set; } = [];
    public IReadOnlyList<CreditNoteListItemViewModel> CreditNotes { get; private set; } = [];
    public string? StoreName { get; private set; }
    public Guid? StoreId { get; private set; }

    // Pagination
    public int CurrentPage { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public int TotalPages { get; private set; }
    public int TotalCount { get; private set; }

    // Pagination helper properties for display
    public int DisplayStartItem => TotalCount > 0 ? (CurrentPage - 1) * PageSize + 1 : 0;
    public int DisplayEndItem => Math.Min(CurrentPage * PageSize, TotalCount);

    // Available statuses for filtering
    public IReadOnlyList<string> AvailableStatuses { get; } = new[]
    {
        "Draft", "Issued", "Paid", "Cancelled", "Corrected"
    };

    // Filter properties bound from query string
    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Year { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    // Tab: invoices or creditnotes
    [BindProperty(SupportsGet = true)]
    public string Tab { get; set; } = "invoices";

    public InvoicesModel(
        ILogger<InvoicesModel> logger,
        CommissionInvoiceService invoiceService,
        StoreService storeService)
    {
        _logger = logger;
        _invoiceService = invoiceService;
        _storeService = storeService;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return RedirectToPage("/Login");
        }

        // Get seller's store
        var store = await _storeService.HandleAsync(new GetStoreBySellerIdQuery(userId), cancellationToken);
        if (store is null)
        {
            _logger.LogWarning("Store not found for seller {SellerId}", userId);
            return Page();
        }

        StoreId = store.Id;
        StoreName = store.Name;
        CurrentPage = Math.Max(1, PageNumber);

        // Parse status filter
        CommissionInvoiceStatus? statusFilter = null;
        if (!string.IsNullOrEmpty(Status) && Enum.TryParse<CommissionInvoiceStatus>(Status, out var parsedStatus))
        {
            statusFilter = parsedStatus;
        }

        if (Tab == "creditnotes")
        {
            // Get credit notes
            var skip = (CurrentPage - 1) * PageSize;
            var creditNotesResult = await _invoiceService.HandleAsync(
                new GetCreditNotesByStoreIdQuery(store.Id, skip, PageSize),
                cancellationToken);

            TotalCount = creditNotesResult.TotalCount;
            TotalPages = creditNotesResult.TotalPages;

            CreditNotes = creditNotesResult.Items.Select(cn => new CreditNoteListItemViewModel(
                cn.Id,
                cn.StoreId,
                cn.StoreName,
                cn.CreditNoteNumber,
                cn.OriginalInvoiceNumber,
                cn.Type,
                cn.Currency,
                cn.GrossAmount,
                cn.IssueDate,
                cn.Reason,
                cn.CreatedAt)).ToList().AsReadOnly();
        }
        else
        {
            // Get invoices
            var invoicesResult = await _invoiceService.HandleAsync(
                new GetCommissionInvoicesQuery(
                    store.Id,
                    Year,
                    null, // Month
                    statusFilter,
                    CurrentPage,
                    PageSize),
                cancellationToken);

            TotalCount = invoicesResult.TotalCount;
            TotalPages = invoicesResult.TotalPages;

            Invoices = invoicesResult.Items.Select(i => new CommissionInvoiceListItemViewModel(
                i.Id,
                i.StoreId,
                i.StoreName,
                i.InvoiceNumber,
                i.Year,
                i.Month,
                i.Status,
                i.Currency,
                i.GrossAmount,
                i.IssueDate,
                i.DueDate,
                i.CreatedAt,
                i.HasCreditNote)).ToList().AsReadOnly();
        }

        _logger.LogInformation("Invoices page accessed for store {StoreId} with {InvoiceCount} invoices",
            store.Id, Invoices.Count);

        return Page();
    }
}
