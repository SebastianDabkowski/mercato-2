using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SD.Project.Application.Queries;
using SD.Project.Application.Services;
using SD.Project.Domain.Entities;
using SD.Project.Filters;

namespace SD.Project.Pages.Api;

/// <summary>
/// API endpoint for downloading credit note PDF.
/// </summary>
[RequireRole(UserRole.Seller, UserRole.Admin)]
public class CreditNotePdfModel : PageModel
{
    private readonly ILogger<CreditNotePdfModel> _logger;
    private readonly CommissionInvoiceService _invoiceService;
    private readonly StoreService _storeService;

    public CreditNotePdfModel(
        ILogger<CreditNotePdfModel> logger,
        CommissionInvoiceService invoiceService,
        StoreService storeService)
    {
        _logger = logger;
        _invoiceService = invoiceService;
        _storeService = storeService;
    }

    public async Task<IActionResult> OnGetAsync(
        [FromRoute] Guid creditNoteId,
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

        // Get credit note
        var creditNote = await _invoiceService.HandleAsync(
            new GetCreditNoteByIdQuery(creditNoteId),
            cancellationToken);

        if (creditNote is null)
        {
            _logger.LogWarning("Credit note not found: {CreditNoteId}", creditNoteId);
            return NotFound();
        }

        // Verify credit note belongs to seller's store (unless admin)
        var isAdmin = User.IsInRole(UserRole.Admin.ToString());
        if (!isAdmin && creditNote.StoreId != store.Id)
        {
            _logger.LogWarning("Credit note {CreditNoteId} does not belong to store {StoreId}", creditNoteId, store.Id);
            return Forbid();
        }

        // Generate PDF
        var pdfBytes = await _invoiceService.GenerateCreditNotePdfAsync(creditNoteId, cancellationToken);
        if (pdfBytes is null)
        {
            return NotFound();
        }

        _logger.LogInformation("Credit note PDF downloaded: {CreditNoteId}", creditNoteId);

        // Return as HTML (the PDF generator creates HTML for browser printing)
        return new ContentResult
        {
            Content = System.Text.Encoding.UTF8.GetString(pdfBytes),
            ContentType = "text/html",
            StatusCode = 200
        };
    }
}
