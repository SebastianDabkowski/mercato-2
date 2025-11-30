using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;

namespace SD.Project.Pages.Api;

/// <summary>
/// API endpoint for downloading invoice PDF.
/// </summary>
[RequireRole(UserRole.Seller, UserRole.Admin)]
public class InvoicePdfModel : PageModel
{
    private readonly ILogger<InvoicePdfModel> _logger;
    private readonly CommissionInvoiceService _invoiceService;
    private readonly StoreService _storeService;

    public InvoicePdfModel(
        ILogger<InvoicePdfModel> logger,
        CommissionInvoiceService invoiceService,
        StoreService storeService)
    {
        _logger = logger;
        _invoiceService = invoiceService;
        _storeService = storeService;
    }

    public async Task<IActionResult> OnGetAsync(
        [FromRoute] Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        // Get seller's store
        var store = await _storeService.HandleAsync(new GetStoreBySellerIdQuery(userId), cancellationToken);
        if (store is null)
        {
            _logger.LogWarning("Store not found for seller {SellerId}", userId);
            return NotFound();
        }

        // Get invoice
        var invoice = await _invoiceService.HandleAsync(
            new GetCommissionInvoiceByIdQuery(invoiceId),
            cancellationToken);

        if (invoice is null)
        {
            _logger.LogWarning("Invoice not found: {InvoiceId}", invoiceId);
            return NotFound();
        }

        // Verify invoice belongs to seller's store (unless admin)
        var isAdmin = User.IsInRole(UserRole.Admin.ToString());
        if (!isAdmin && invoice.StoreId != store.Id)
        {
            _logger.LogWarning("Invoice {InvoiceId} does not belong to store {StoreId}", invoiceId, store.Id);
            return Forbid();
        }

        // Check invoice status allows download
        if (invoice.Status == "Draft")
        {
            return BadRequest("Cannot download draft invoice");
        }

        // Generate PDF
        var pdfBytes = await _invoiceService.GenerateInvoicePdfAsync(invoiceId, cancellationToken);
        if (pdfBytes is null)
        {
            return NotFound();
        }

        _logger.LogInformation("Invoice PDF downloaded: {InvoiceId}", invoiceId);

        // Return as HTML (the PDF generator creates HTML for browser printing)
        return new ContentResult
        {
            Content = System.Text.Encoding.UTF8.GetString(pdfBytes),
            ContentType = "text/html",
            StatusCode = 200
        };
    }
}
