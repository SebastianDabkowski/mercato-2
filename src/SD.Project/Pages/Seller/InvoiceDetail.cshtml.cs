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
/// Page model for displaying commission invoice details.
/// </summary>
[RequireRole(UserRole.Seller, UserRole.Admin)]
public class InvoiceDetailModel : PageModel
{
    private readonly ILogger<InvoiceDetailModel> _logger;
    private readonly CommissionInvoiceService _invoiceService;
    private readonly StoreService _storeService;

    public CommissionInvoiceDetailsViewModel? Invoice { get; private set; }

    [BindProperty(SupportsGet = true)]
    public Guid InvoiceId { get; set; }

    public InvoiceDetailModel(
        ILogger<InvoiceDetailModel> logger,
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
            return RedirectToPage("/Seller/Invoices");
        }

        // Get invoice details
        var invoiceDetails = await _invoiceService.HandleAsync(
            new GetCommissionInvoiceDetailsQuery(InvoiceId),
            cancellationToken);

        if (invoiceDetails is null)
        {
            _logger.LogWarning("Invoice not found: {InvoiceId}", InvoiceId);
            return NotFound();
        }

        // Verify invoice belongs to seller's store
        if (invoiceDetails.StoreId != store.Id)
        {
            _logger.LogWarning("Invoice {InvoiceId} does not belong to store {StoreId}", InvoiceId, store.Id);
            return Forbid();
        }

        // Map to view model
        Invoice = new CommissionInvoiceDetailsViewModel(
            invoiceDetails.Id,
            invoiceDetails.StoreId,
            invoiceDetails.SellerId,
            invoiceDetails.SettlementId,
            invoiceDetails.InvoiceNumber,
            invoiceDetails.Year,
            invoiceDetails.Month,
            invoiceDetails.Status,
            invoiceDetails.Currency,
            invoiceDetails.NetAmount,
            invoiceDetails.TaxRate,
            invoiceDetails.TaxAmount,
            invoiceDetails.GrossAmount,
            invoiceDetails.IssueDate,
            invoiceDetails.DueDate,
            invoiceDetails.PeriodStart,
            invoiceDetails.PeriodEnd,
            invoiceDetails.SellerName,
            invoiceDetails.SellerTaxId,
            invoiceDetails.SellerAddress,
            invoiceDetails.SellerCity,
            invoiceDetails.SellerPostalCode,
            invoiceDetails.SellerCountry,
            invoiceDetails.IssuerName,
            invoiceDetails.IssuerTaxId,
            invoiceDetails.IssuerAddress,
            invoiceDetails.IssuerCity,
            invoiceDetails.IssuerPostalCode,
            invoiceDetails.IssuerCountry,
            invoiceDetails.Lines.Select(l => new CommissionInvoiceLineViewModel(
                l.Id, l.Description, l.Quantity, l.UnitPrice, l.TaxRate, l.NetAmount, l.TaxAmount, l.GrossAmount)).ToList(),
            invoiceDetails.CreditNotes?.Select(cn => new CreditNoteListItemViewModel(
                cn.Id, cn.StoreId, cn.StoreName, cn.CreditNoteNumber, cn.OriginalInvoiceNumber,
                cn.Type, cn.Currency, cn.GrossAmount, cn.IssueDate, cn.Reason, cn.CreatedAt)).ToList(),
            invoiceDetails.Notes,
            invoiceDetails.CorrectedByNoteId,
            invoiceDetails.CreatedAt,
            invoiceDetails.IssuedAt,
            invoiceDetails.PaidAt,
            invoiceDetails.CancelledAt);

        _logger.LogInformation("Invoice detail page accessed for invoice {InvoiceId}", InvoiceId);

        return Page();
    }
}
